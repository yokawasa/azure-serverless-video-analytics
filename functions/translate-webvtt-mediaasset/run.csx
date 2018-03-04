#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Runtime.Serialization"
#r "System.Web"

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

private static CloudMediaContext _context = null;
private static readonly string _amsAADTenantDomain = Environment.GetEnvironmentVariable("AMSAADTenantDomain");
private static readonly string _amsRestApiEndpoint = Environment.GetEnvironmentVariable("AMSRestApiEndpoint");
private static readonly string _amsClientId = Environment.GetEnvironmentVariable("AMSClientId");
private static readonly string _amsClientSecret = Environment.GetEnvironmentVariable("AMSClientSecret");
private static readonly string _amsStorageAccountName = Environment.GetEnvironmentVariable("AMSStorageAccountName");
private static readonly string _amsStorageAccountKey = Environment.GetEnvironmentVariable("AMSStorageAccountKey");
private static readonly string _translatorAPIKey = Environment.GetEnvironmentVariable("TranslatorAPIKey");

private static HttpClient client = new HttpClient();

public static void Run(string myQueueItem, TraceWriter log)
{
    log.Info($"queueTrigger Function was triggered!");
    string jsonContent = myQueueItem;
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    log.Info("Request : " + jsonContent);
       
    string assetid = data["Azure Media Indexer 2 Preview"].AssetId;
    string srcLang = data.SourceLanguage;
    string[] transLanguages = data.TranslatedLanguages.ToObject<string[]>();
    string callbackUrl = data.CallbackUrl;
    log.Info("Input - callbackUrl : " + callbackUrl);

    try
    {
        // Load AMS account context
        log.Info($"Using Azure Media Service Rest API Endpoint : {_amsRestApiEndpoint}");
        AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_amsAADTenantDomain,
            new AzureAdClientSymmetricKey(_amsClientId, _amsClientSecret),
            AzureEnvironments.AzureCloudEnvironment);
        AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
        _context = new CloudMediaContext(new Uri(_amsRestApiEndpoint), tokenProvider);

        // Get the Asset
        var asset = _context.Assets.Where(a => a.Id == assetid).FirstOrDefault();
        if (asset == null)
        {
            log.Info("Asset not found - " + assetid);
            //return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset not found" });
            //FIXME: What should I return??
            client.PostAsJsonAsync<string>(callbackUrl, "NG");
            return;
        }
        log.Info("Asset found, AssetId : " + asset.Id);
        //Wait enough for blobs created in previous STT operation are flushed
        Thread.Sleep(TimeSpan.FromSeconds(30));

        // Add AssetFiles to the Asset
        CloudBlobContainer destinationBlobContainer = GetCloudBlobContainer(_amsStorageAccountName, _amsStorageAccountKey, asset.Uri.Segments[1]);
        string blobPrefix = null;
        bool useFlatBlobListing = true;
        var blobList = destinationBlobContainer.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.None);
        CloudBlockBlob blob = null;
        foreach (var b in blobList)
        {
            string blobUri = (b as CloudBlob).Uri.ToString();
            if (System.Text.RegularExpressions.Regex.IsMatch(blobUri, @"SpReco[.]vtt$"))
            {
                log.Info("Found WebVTT blob : " + blobUri);
                blob = (CloudBlockBlob)b;
            }
        }
        if (blob != null)
        {
            string filename = "subtitle";
            Translator(asset, blob, filename, destinationBlobContainer, srcLang, transLanguages, log);
        }
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        throw ex;
    }
    
    string result = jsonContent;
    //Async POST with JSON format
    client.PostAsJsonAsync<string>(callbackUrl, result);
}

static public CloudBlobContainer GetCloudBlobContainer(string storageAccountName, string storageAccountKey, string containerName)
{
    CloudStorageAccount sourceStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
    CloudBlobClient sourceCloudBlobClient = sourceStorageAccount.CreateCloudBlobClient();
    return sourceCloudBlobClient.GetContainerReference(containerName);
}

static public void Translator(IAsset asset, CloudBlockBlob myBlob, string fileName, CloudBlobContainer outContainer, string srcLang, string[] languages, TraceWriter log)
{
    // yoichika: change it so as to read api key from env variable
    var authTokenSource = new AzureAuthToken(_translatorAPIKey);
    string authToken;
    try
    {
        authToken = authTokenSource.GetAccessToken();
    }
    catch (HttpRequestException ex)
    {
        if (authTokenSource.RequestStatusCode == HttpStatusCode.Unauthorized)
        {
            log.Info("Request to token service is not authorized (401). Check that the Azure subscription key is valid.");
        }
        if (authTokenSource.RequestStatusCode == HttpStatusCode.Forbidden)
        {
            log.Info("Request to token service is not authorized (403). For accounts in the free-tier, check that the account quota is not exceeded.");
        }
        throw ex;
    }

    try {
        //string[] languages = new string[] { "ms", "th", "vi", "zh-Hans", "zh-Hant" };
        foreach (string lang in languages)
        {
            string text = "";
            using (var stream = myBlob.OpenRead())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        text = text + Translate(reader.ReadLine(), srcLang, lang, authToken) + "\r\n";
                    }
                }
            }

            string outputFile = fileName + @"-" + lang + @".vtt";
            outContainer.CreateIfNotExists();
            CloudBlockBlob blob = outContainer.GetBlockBlobReference(outputFile);
            blob.DeleteIfExists();
            
            var options = new BlobRequestOptions()
            {
                ServerTimeout = TimeSpan.FromMinutes(10)
            };
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false))
            {
                blob.UploadFromStream(stream, null, options);
            }
            log.Info("Uploaded : " + outputFile);
            
            IAssetFile assetFile = asset.AssetFiles.Create(outputFile);
            blob.FetchAttributes();
            assetFile.ContentFileSize = blob.Properties.Length;
            assetFile.IsPrimary = false;
            assetFile.Update();
            log.Info("Asset file registered : " + assetFile.Name);
        }
    }
    catch(Exception ex)
    {
        log.Error("ERROR: failed.");
        log.Info($"StackTrace : {ex.StackTrace}");
        throw ex;
    }
}

static string Translate(string text, string fromLang, string toLang, string authToken)
{
    if (text == "WEBVTT" || text == "") return text;
    if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[0-9][0-9]:[0-9][0-9]:[0-9][0-9][.][0-9][0-9][0-9][ ]-->[ ][0-9][0-9]:[0-9][0-9]:[0-9][0-9][.][0-9][0-9][0-9]")) return text;
    if (System.Text.RegularExpressions.Regex.IsMatch(text, @"NOTE[ ]Confidence[:]*")) return text;

    string translation;
    string uri = "https://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + HttpUtility.UrlEncode(text) + "&from=" + fromLang + "&to=" + toLang;

    HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
    httpWebRequest.Headers.Add("Authorization", authToken);

    using (WebResponse response = httpWebRequest.GetResponse())
    using (Stream stream = response.GetResponseStream())
    {
        DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
        translation = (string)dcs.ReadObject(stream);
    }
    return translation;
}


public class AzureAuthToken
{
    /// URL of the token service
    private static readonly Uri ServiceUrl = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/issueToken");
    
    /// Name of header used to pass the subscription key to the token service
    private const string OcpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
    
    /// After obtaining a valid token, this class will cache it for this duration.
    /// Use a duration of 5 minutes, which is less than the actual token lifetime of 10 minutes.
    private static readonly TimeSpan TokenCacheDuration = new TimeSpan(0, 5, 0);
    
    /// Cache the value of the last valid token obtained from the token service.
    private string _storedTokenValue = string.Empty;
    
    /// When the last valid token was obtained.
    private DateTime _storedTokenTime = DateTime.MinValue;
    
    /// Gets the subscription key.
    public string SubscriptionKey { get; }
    /// Gets the HTTP status code for the most recent request to the token service.
    public HttpStatusCode RequestStatusCode { get; private set; }
    
    /// <summary>
    /// Creates a client to obtain an access token.
    /// </summary>
    /// <param name="key">Subscription key to use to get an authentication token.</param>
    public AzureAuthToken(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "A subscription key is required");
        }
        this.SubscriptionKey = key;
        this.RequestStatusCode = HttpStatusCode.InternalServerError;
    }
    
    /// <summary>
    /// Gets a token for the specified subscription.
    /// </summary>
    /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
    /// <remarks>
    /// This method uses a cache to limit the number of request to the token service.
    /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
    /// request to the token service, this method caches the access token. Subsequent 
    /// invocations of the method return the cached token for the next 5 minutes. After
    /// 5 minutes, a new token is fetched from the token service and the cache is updated.
    /// </remarks>
    public async Task<string> GetAccessTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(this.SubscriptionKey))
        {
            return string.Empty;
        }
        // Re-use the cached token if there is one.
        if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
        {
            return _storedTokenValue;
        }

        using (var client = new HttpClient())
        using (var request = new HttpRequestMessage())
        {
            request.Method = HttpMethod.Post;
            request.RequestUri = ServiceUrl;
            request.Content = new StringContent(string.Empty);
            request.Headers.TryAddWithoutValidation(OcpApimSubscriptionKeyHeader, this.SubscriptionKey);
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = await client.SendAsync(request);
            this.RequestStatusCode = response.StatusCode;
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadAsStringAsync();
            _storedTokenTime = DateTime.Now;
            _storedTokenValue = "Bearer " + token;
            return _storedTokenValue;
        }
    }
    
    /// <summary>
    /// Gets a token for the specified subscription. Synchronous version.
    /// Use of async version preferred
    /// </summary>
    /// <returns>The encoded JWT token prefixed with the string "Bearer ".</returns>
    /// <remarks>
    /// This method uses a cache to limit the number of request to the token service.
    /// A fresh token can be re-used during its lifetime of 10 minutes. After a successful
    /// request to the token service, this method caches the access token. Subsequent 
    /// invocations of the method return the cached token for the next 5 minutes. After
    /// 5 minutes, a new token is fetched from the token service and the cache is updated.
    /// </remarks>
    public string GetAccessToken()
    {
        // Re-use the cached token if there is one.
        if ((DateTime.Now - _storedTokenTime) < TokenCacheDuration)
        {
            return _storedTokenValue;
        }
        string accessToken = null;
        var task = Task.Run(async () =>
        {
            accessToken = await this.GetAccessTokenAsync();
        });
        while (!task.IsCompleted)
        {
            System.Threading.Thread.Yield();
        }
        if (task.IsFaulted)
        {
            throw task.Exception;
        }
        if (task.IsCanceled)
        {
            throw new Exception("Timeout obtaining access token.");
        }
        return accessToken;
    }
}
