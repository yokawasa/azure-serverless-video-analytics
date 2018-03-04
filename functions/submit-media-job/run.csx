#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
private static readonly string _amsSkipMBREncoding = Environment.GetEnvironmentVariable("AMSSkipMBREncoding");

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
    if (data.Tasks == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass Tasks in the input object" });
    log.Info("Input - Tasks : " + data.Tasks);
    
    string assetid = data.AssetId;
    string[] tasks = data.Tasks.ToObject<string[]>();
      
    IJob job = null;
    List<MediaTask> mediaTaskList = new List<MediaTask>();
    //IAsset outputAsset = null;
    //IAsset outputEncoding = null;
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
        log.Info("Asset found, AssetId : " + asset.Id);
        
        int taskindex = 0;
        // Declare a new Media Processing job
        job = _context.Jobs.Create("Azure Functions - Media Processing Job - " + assetid);
        foreach (string proc in tasks)
        {
            log.Info("Executing Tasks : " + proc);
            MediaTask t = new MediaTask();
            t.index = taskindex;
            t.processorName = proc;
            AddTask(job, asset, proc, null, false);
            mediaTaskList.Add(t);
            taskindex++;
        }
        job.Submit();
        log.Info("Job Submitted");
        
    }
    catch (Exception ex)
    {
        log.Info("Exception " + ex);
        return req.CreateResponse(HttpStatusCode.BadRequest);
    }
    
    JObject j = new JObject();
    j["JobId"] = job.Id;
    List<string> assets = new List<string>();
    foreach (var t in mediaTaskList)
    {
        t.outputAsset = job.OutputMediaAssets[t.index];
        t.task = job.Tasks[t.index];
        var jt = new JObject();
        jt["AssetId"] = t.outputAsset.Id;
        jt["TasktId"] = t.task.Id;
        j[t.processorName] = jt;
        assets.Add((string)(t.outputAsset.Id));
    }
    log.Info("test : " + job.Id);

    // yoichika: Added Source Media AssetId
    assets.Add(assetid);

    var array = JArray.FromObject(assets);
    j["AssetIds"] = array;
    
    log.Info("Job Id: " + job.Id);
    foreach (var t in mediaTaskList)
    {
        log.Info("Task : " + t.processorName + " (" + t.outputAsset.Id + ")");
    }    
    return req.CreateResponse(HttpStatusCode.OK, j);
}

public class MediaTask
{
    public int index { get; set; }
    public string processorName { get; set; }
    public IAsset outputAsset { get; set; }
    public ITask task { get; set; }
}

static public IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
{
    var processor = _context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
    ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

    if (processor == null)
        throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

    return processor;
}

static public IAsset AddTask(IJob job, IAsset sourceAsset, string processor, string presetString, bool isPresetFile, int priority = 10)
{
    // Get a media processor reference,
    // and pass to it the name of the processor to use for the specific task.
    IMediaProcessor mediaProcessor = GetLatestMediaProcessorByName(processor);
    
    if (presetString == null) {
        switch (processor) {
            case "Media Encoder Standard":
                //Switching preset file depending if it's AMSSkipMBREncoding is enabled
                if ( Int32.Parse(_amsSkipMBREncoding) != 1 ) {
                    presetString = "encoding-mbr-thumb.json";                
                } else {
                    presetString = "encoding-thumb.json";
                }
                isPresetFile = true;
                break;
            case "Azure Media Indexer 2 Preview":
                presetString = "Indexer-v2.json";
                isPresetFile = true;
                break;
            default:
                break;
        }
    }
    
    string taskConfiguration = null;
    if (isPresetFile)
    {
        string repoPath = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process);
        string presetPath;
        if (repoPath == null)
        {
            presetPath = @"Presets/" + presetString;
        }
        else
        {
            presetPath = Path.Combine(repoPath, @"site\wwwroot\Presets\" + presetString);
        }
        taskConfiguration = File.ReadAllText(presetPath);
    }
    else {
        taskConfiguration = presetString;        
    }
    
    // Create a task with the encoding details, using a string preset.
    var task = job.Tasks.AddNew("Azure Functions:" + processor + " task", mediaProcessor, taskConfiguration, TaskOptions.None);
    task.Priority = priority;
    
    // Specify the input asset to be indexed.
    task.InputAssets.Add(sourceAsset);
    
    IAsset outputAsset = null;
    // Add an output asset to contain the results of the job.
    outputAsset = task.OutputAssets.AddNew(sourceAsset.Name + " (Processed by Video Cognitive Workflow)", AssetCreationOptions.None);
    
    return outputAsset;
}

public static string ReturnId(IJob job, int index)
{
    return index > -1 ? job.OutputMediaAssets[index].Id : null;
}

public static string ReturnTaskId(IJob job, int index)
{
    return index > -1 ? job.Tasks[index].Id : null;
}
