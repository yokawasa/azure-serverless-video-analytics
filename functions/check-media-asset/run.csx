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

    // Validate input objects
    if (data.QueueMessage == null)
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    log.Info("Input - QueueMessage : " + data.QueueMessage);

    dynamic queueMessage = JsonConvert.DeserializeObject((string)(data.QueueMessage));
    if (queueMessage.SourceLocation == null)
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    if (queueMessage.TargetLocation == null)
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    log.Info("Input - SourceLocation : " + queueMessage.SourceLocation);
    log.Info("Input - TargetLocation : " + queueMessage.TargetLocation);
    
    string storageUrl = (string)(queueMessage.TargetLocation);
    log.Info("Asset Storage URL - " + storageUrl);
    string assetid = null;
    string filename = null;
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
        Uri storageUri = new Uri(storageUrl);
        string assetPath = Path.GetFileName(storageUri.AbsolutePath);
        string assetid2 = "nb:cid:UUID:" + assetPath.Substring(assetPath.IndexOf('-', 1) + 1);
        log.Info("AssetId - " + assetid2);
        //IAsset asset = _context.Assets.Where(a => a.id == assetid2).FirstOrDefault();
        var asset = _context.Assets.Where(a => a.Id == assetid2).FirstOrDefault();
        if (asset == null)
        {
            log.Info("Asset not found - " + assetid);
            return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset not found" });
        }
        log.Info("Asset found, AssetId : " + asset.Id);

        assetid = asset.Id;
        filename = Path.GetFileName((string)(queueMessage.SourceLocation));
        
        log.Info("Output - AssetId : " + assetid);
        log.Info("Output - FileName : " + filename);
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    }

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        AssetId = assetid,
        FileName = filename
    });
}