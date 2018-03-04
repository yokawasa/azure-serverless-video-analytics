#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

private static CloudMediaContext _context = null;
private static readonly string _amsAADTenantDomain = Environment.GetEnvironmentVariable("AMSAADTenantDomain");
private static readonly string _amsRestApiEndpoint = Environment.GetEnvironmentVariable("AMSRestApiEndpoint");
private static readonly string _amsClientId = Environment.GetEnvironmentVariable("AMSClientId");
private static readonly string _amsClientSecret = Environment.GetEnvironmentVariable("AMSClientSecret");
private static readonly string _amsStorageAccountName = Environment.GetEnvironmentVariable("AMSStorageAccountName");
private static readonly string _amsStorageAccountKey = Environment.GetEnvironmentVariable("AMSStorageAccountKey");
private static readonly string _srcStorageAccountName = Environment.GetEnvironmentVariable("SourceStorageAccountName");
private static readonly string _srcStorageAccountKey = Environment.GetEnvironmentVariable("SourceStorageAccountKey");

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    log.Info("Request : " + jsonContent);

    // Validate input objects
    if (data.FileName == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass FileName in the input object" });
    log.Info("Input - File Name : " + data.FileName);
    
    string path = data.FileName;
    Blob blob = new Blob(path);
    string name = blob.name;
    log.Info("Input - Blob Name : " + name);
    
    IAsset newAsset = null;
    //IIngestManifest manifest = null;
    try
    {
        // Load AMS account context
        log.Info($"Using Azure Media Service Rest API Endpoint : {_amsRestApiEndpoint}");
        AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_amsAADTenantDomain,
            new AzureAdClientSymmetricKey(_amsClientId, _amsClientSecret),
            AzureEnvironments.AzureCloudEnvironment);
        AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
        _context = new CloudMediaContext(new Uri(_amsRestApiEndpoint), tokenProvider);
        
        // Create Asset
        string assetName = name;
        newAsset = _context.Assets.Create(assetName, AssetCreationOptions.None);
        log.Info("Created Azure Media Services Asset : ");
        log.Info("  - Asset Name = " + name);
        log.Info("  - Asset Creation Option = " + AssetCreationOptions.None);
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    }
    
    log.Info("Asset ID : " + newAsset.Id);
    
    return req.CreateResponse(HttpStatusCode.OK, new
    {
        AssetId = newAsset.Id,
        DestinationContainer = newAsset.Uri.Segments[1]
    });
}

public class Blob
{
    public string containerName { get; set; }
    public string name { get; set; }
    
    public Blob(string path)
    {
        containerName = path.Substring(1, (path.IndexOf('/', 1) - 1));
        name = path.Substring(path.IndexOf('/', 1) + 1);    
    }
}
