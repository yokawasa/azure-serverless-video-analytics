#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

private static CloudMediaContext _context = null;
private static readonly string _amsAADTenantDomain = Environment.GetEnvironmentVariable("AMSAADTenantDomain");
private static readonly string _amsRestApiEndpoint = Environment.GetEnvironmentVariable("AMSRestApiEndpoint");
private static readonly string _amsClientId = Environment.GetEnvironmentVariable("AMSClientId");
private static readonly string _amsClientSecret = Environment.GetEnvironmentVariable("AMSClientSecret");
private static readonly string _amsStorageAccountName = Environment.GetEnvironmentVariable("AMSStorageAccountName");
private static readonly string _amsStorageAccountKey = Environment.GetEnvironmentVariable("AMSStorageAccountKey");

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    log.Info("Request : " + jsonContent);

    // Validate input objects
    if (data.AssetId == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass AssetId in the input object" });
    log.Info("Input - AssetId : " + data.AssetId);
    if (data.CopyFileName == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass CopyFileName in the input object" });
    log.Info("Input - CopyFileName : " + data.CopyFileName);

    string assetid = data.AssetId;
    string fileName = data.CopyFileName;
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
            return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Asset not found" });
        }
        log.Info("Asset found, Asset ID : " + asset.Id);

        // Add AssetFiles to the Asset
        CloudBlobContainer destinationBlobContainer = GetCloudBlobContainer(_amsStorageAccountName, _amsStorageAccountKey, asset.Uri.Segments[1]);
        IAssetFile assetFile = asset.AssetFiles.Create(fileName);
        CloudBlockBlob blob = destinationBlobContainer.GetBlockBlobReference(fileName);
        blob.FetchAttributes();
        assetFile.ContentFileSize = blob.Properties.Length;
        assetFile.IsPrimary = true;
        assetFile.Update();
        log.Info("Asset file registered : " + assetFile.Name);
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    }
    log.Info("Asset ID : " + assetid);
    
    return req.CreateResponse(HttpStatusCode.OK, new
    {
        AssetId = assetid
    });
}

static public CloudBlobContainer GetCloudBlobContainer(string storageAccountName, string storageAccountKey, string containerName)
{
    CloudStorageAccount sourceStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
    CloudBlobClient sourceCloudBlobClient = sourceStorageAccount.CreateCloudBlobClient();
    return sourceCloudBlobClient.GetContainerReference(containerName);
}