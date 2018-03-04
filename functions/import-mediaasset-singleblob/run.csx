#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.Threading.Tasks;
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
    log.Info("Input - FileName : " + data.FileName);
    if (data.DestinationContainer == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass DestinationContainer in the input object" });
    log.Info("Input - DestinationContainer : " + data.DestinationContainer);
    
    string path = data.FileName;
    Blob blob = new Blob(path);
    string srcContainerName = blob.containerName;
    string name = blob.name;
    log.Info("Input - Blob Container Name : " + srcContainerName);
    log.Info("Input - Blob Name : " + name);
    string destContainerName = data.DestinationContainer;
    
    IAsset newAsset = null;
    //IIngestManifest manifest = null;
    try
    {
        // Setup blob container
        CloudBlobContainer sourceBlobContainer = GetCloudBlobContainer(_srcStorageAccountName, _srcStorageAccountKey, srcContainerName);
        CloudBlobContainer destinationBlobContainer = GetCloudBlobContainer(_amsStorageAccountName, _amsStorageAccountKey, destContainerName);
        //sourceBlobContainer.CreateIfNotExists();
        CloudBlockBlob myBlob = sourceBlobContainer.GetBlockBlobReference(name);
        // Copy Source Blob container into Destination Blob container that is associated with the asset.
        CopyBlobsAsync(myBlob, destinationBlobContainer, log);
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
    }
    
    return req.CreateResponse(HttpStatusCode.OK, new
    {
        CopyFileName = name,
        SourceContainer = srcContainerName
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

static public CloudBlobContainer GetCloudBlobContainer(string storageAccountName, string storageAccountKey, string containerName)
{
    CloudStorageAccount sourceStorageAccount = new CloudStorageAccount(new StorageCredentials(storageAccountName, storageAccountKey), true);
    CloudBlobClient sourceCloudBlobClient = sourceStorageAccount.CreateCloudBlobClient();
    return sourceCloudBlobClient.GetContainerReference(containerName);
}

static public void CopyBlobsAsync(CloudBlockBlob sourceBlob, CloudBlobContainer destinationBlobContainer, TraceWriter log)
{
    if (destinationBlobContainer.CreateIfNotExists())
    {
        destinationBlobContainer.SetPermissions(new BlobContainerPermissions
        {
            PublicAccess = BlobContainerPublicAccessType.Blob
        });
    }

    string blobPrefix = null;
    bool useFlatBlobListing = true;
    
    log.Info("Source blob : " + (sourceBlob as CloudBlob).Uri.ToString());
    CloudBlob destinationBlob = destinationBlobContainer.GetBlockBlobReference((sourceBlob as CloudBlob).Name);
    if (destinationBlob.Exists())
    {
         log.Info("Destination blob already exists. Skipping: " + destinationBlob.Uri.ToString());
    }
    else
    {
        log.Info("Copying blob " + sourceBlob.Uri.ToString() + " to " + destinationBlob.Uri.ToString());
        CopyBlobAsync(sourceBlob as CloudBlob, destinationBlob);
    }
}

static public async void CopyBlobAsync(CloudBlob sourceBlob, CloudBlob destinationBlob)
{
    var signature = sourceBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy
    {
        Permissions = SharedAccessBlobPermissions.Read,
        SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24)
    });
    await destinationBlob.StartCopyAsync(new Uri(sourceBlob.Uri.AbsoluteUri + signature));
}
