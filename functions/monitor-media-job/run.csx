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
    int delay = 15000;
    if (data.JobId == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass JobId in the input object" });
    if (data.Delay != null)
        delay = data.Delay;
    log.Info("Input - JobId : " + data.JobId);
    
    log.Info($"Wait " + delay + "(ms)");
    System.Threading.Thread.Sleep(delay);
    
    IJob job = null;
    try
    {
        // Load AMS account context
        log.Info($"Using Azure Media Service Rest API Endpoint : {_amsRestApiEndpoint}");
        AzureAdTokenCredentials tokenCredentials = new AzureAdTokenCredentials(_amsAADTenantDomain,
            new AzureAdClientSymmetricKey(_amsClientId, _amsClientSecret),
            AzureEnvironments.AzureCloudEnvironment);
        AzureAdTokenProvider tokenProvider = new AzureAdTokenProvider(tokenCredentials);
        _context = new CloudMediaContext(new Uri(_amsRestApiEndpoint), tokenProvider);

        // Get the job
        string jobid = (string)data.JobId;
        job = _context.Jobs.Where(j => j.Id == jobid).FirstOrDefault();
        if (job == null)
        {
            log.Info("Job not found : " + jobid);
            return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Job not found" });
        }
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }

    // IJob.State
    // - Queued = 0
    // - Scheduled = 1
    // - Processing = 2
    // - Finished = 3
    // - Error = 4
    // - Canceled = 5
    // - Canceling = 6
    log.Info($"Job {job.Id} status is {job.State}");

    return req.CreateResponse(HttpStatusCode.OK, new
    {
        JobId = job.Id,
        JobState = job.State
    });
}

