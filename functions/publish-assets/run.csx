#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Text.RegularExpressions;

private static CloudMediaContext _context = null;
private static readonly string _amsAADTenantDomain = Environment.GetEnvironmentVariable("AMSAADTenantDomain");
private static readonly string _amsRestApiEndpoint = Environment.GetEnvironmentVariable("AMSRestApiEndpoint");
private static readonly string _amsClientId = Environment.GetEnvironmentVariable("AMSClientId");
private static readonly string _amsClientSecret = Environment.GetEnvironmentVariable("AMSClientSecret");
private static readonly string _amsStorageAccountName = Environment.GetEnvironmentVariable("AMSStorageAccountName");
private static readonly string _amsStorageAccountKey = Environment.GetEnvironmentVariable("AMSStorageAccountKey");
private static readonly string _amsSkipMBREncoding = Environment.GetEnvironmentVariable("AMSSkipMBREncoding");

public class Meta
{
    public string id;
    public string name;
    public string asset_name;
    public string thumbnail_url;
    public string manifest_url;
    public string lang;
    public string webvtt_url;
    public List<string> key_phrases;
    public List<Webvtt> subtitle_urls;
}

public class Webvtt
{
    public string lang;
    public string webvtt_url;
}


public static async Task<object> Run(HttpRequestMessage req, IAsyncCollector<object> outputDocument, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    log.Info("Request : " + jsonContent);
    
    // Validate input objects
    if (data.AssetIds == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass AssetIds in the input object" });
    log.Info("Input - AssetIds : " + data.AssetIds);

    string[] assetids = data.AssetIds.ToObject<string[]>();
    string streamingUrl = "";
    
    // Initialize Meta
    Meta meta = new Meta();
    meta.key_phrases = new List<string>();
    meta.subtitle_urls = new List<Webvtt>();

    try
    {
        // Load AMS account context
        log.Info($"Using Azure Media Service Rest API Endpoint : {_amsRestApiEndpoint}");
        AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_amsAADTenantDomain,
            new AzureAdClientSymmetricKey(_amsClientId, _amsClientSecret),
            AzureEnvironments.AzureCloudEnvironment);
        AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
        _context = new CloudMediaContext(new Uri(_amsRestApiEndpoint), tokenProvider);

        foreach (string assetid in assetids)
        {
            // Get the Asset
            var asset = _context.Assets.Where(a => a.Id == assetid).FirstOrDefault();
            if (asset == null)
            {
                log.Info("Asset not found - " + assetid);
                return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset not found" });
            }
            log.Info("Asset found, Asset ID : " + asset.Id);
            
            // Publish with a streaming locator: 
            // Access Permission = 365 * 10 days 
            IAccessPolicy streamingAccessPolicy = _context.AccessPolicies.Create("streamingAccessPolicy", TimeSpan.FromDays(365*10), AccessPermissions.Read);
            ILocator outputLocator = _context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset, streamingAccessPolicy, DateTime.UtcNow.AddMinutes(-5));
            
            var manifestFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();
            if (manifestFile != null && outputLocator != null)
            {
                streamingUrl = outputLocator.Path + manifestFile.Name + "/manifest";
                meta.id = assetid.Substring(12);
                meta.manifest_url = streamingUrl;
                meta.name = asset.Name;
                meta.asset_name = asset.Name;
                log.Info("Streaming URL : " + streamingUrl);
            }

            // Get thumbnail_url
            var thumbnailFile = asset.AssetFiles.Where(f => f.Name.ToLower().EndsWith(".png")).FirstOrDefault();
            if (thumbnailFile != null && outputLocator != null) {
                meta.thumbnail_url = outputLocator.Path + thumbnailFile.Name;
                log.Info("thumbnail url : " + meta.thumbnail_url);                     
            }

            // Get caption and subtitle webvtts in the output asset
            IEnumerable<IAssetFile> webvtts = asset
                    .AssetFiles
                    .ToList()
                    .Where(af => af.Name.EndsWith(".vtt", StringComparison.OrdinalIgnoreCase));

            foreach(IAssetFile af in webvtts)
            {
                var filename = af.Name;
                if (filename.EndsWith("_SpReco.vtt", StringComparison.OrdinalIgnoreCase) ) {
                    meta.webvtt_url = outputLocator.Path + filename;
                    meta.lang="en";
                    log.Info("webvtt url : " + meta.webvtt_url);
                }
                if (filename.StartsWith("subtitle", StringComparison.OrdinalIgnoreCase) ) {
                    
                    Match matched = Regex.Match(filename, @"subtitle-(.*)\.vtt");
                    if (matched.Success)
                    {
                        var matched_lang = matched.Groups[1].Value;
                        Webvtt webvtt = new Webvtt();
                        webvtt.lang = matched_lang;
                        webvtt.webvtt_url = outputLocator.Path + filename;
                        log.Info("subtitle webvtt url : " + webvtt.webvtt_url );                        
                        meta.subtitle_urls.Add(webvtt);
                    }
                }
            }
        }

        // Saving meta into Cosmos DB via CosmosDB out binding
        await outputDocument.AddAsync(meta);
        //Wait enough for cosmosdb flushed perfectly
        Thread.Sleep(TimeSpan.FromSeconds(10)); 

        return req.CreateResponse(HttpStatusCode.OK, new
        {
            ContentId = meta.id
        });
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
}
