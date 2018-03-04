#r "Newtonsoft.Json"
#r "Microsoft.Azure.Documents.Client"

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

private static readonly string _cosmosDBAccountName = Environment.GetEnvironmentVariable("CosmosDBAccountName");
private static readonly string _cosmosDBAccountKey = Environment.GetEnvironmentVariable("CosmosDBAccountKey");
private static readonly string _azureSearchServiceName = Environment.GetEnvironmentVariable("AzureSearchServiceName");
private static readonly string _azureSearchAdminKey = Environment.GetEnvironmentVariable("AzureSearchAdminKey");
private static SearchServiceClient _searchClient;

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

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log) 
{
    log.Info($"Webhook was triggered!");
    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    if (data.ContentId == null)
        return new HttpResponseMessage(HttpStatusCode.BadRequest);
        
    string contentId = data.ContentId;

    try
    {
        MainAsync(contentId,log).Wait();
    }
    catch (DocumentClientException de)
    {
        Exception baseException = de.GetBaseException();
        log.Info(string.Format("{0} error occurred: {1}", de.StatusCode, de));
    }
    catch (Exception e)
    {
        log.Info(string.Format("Error: {0}", e));
    }

    return req.CreateResponse(HttpStatusCode.OK);
}

static async Task MainAsync(string contentId, TraceWriter log)
{
    DocumentClient dbclient = new DocumentClient(
        new Uri(string.Format("https://{0}.documents.azure.com:443/", _cosmosDBAccountName)), 
        _cosmosDBAccountKey
    );
    var response = await dbclient.ReadDocumentAsync(
                UriFactory.CreateDocumentUri("asset", "meta", contentId)
            );

    var meta = JsonConvert.DeserializeObject<Meta>(response.Resource.ToString());
    var webvtts = new List<Webvtt>();
    webvtts.Add(
            new Webvtt
            {
                lang = "en",
                webvtt_url = meta.webvtt_url
            }
        );
    log.Info(string.Format("target webvtt={0} (lang:{1})", meta.webvtt_url, "en"));

    List<Webvtt> subtitle_urls = (List<Webvtt>) meta.subtitle_urls;
    foreach (Webvtt w in subtitle_urls )
    {
        log.Info(string.Format("target webvtt={0} (lang:{1})", w.webvtt_url, w.lang));
        webvtts.Add(
                new Webvtt
                {
                    lang = w.lang,
                    webvtt_url = w.webvtt_url
                }
            );
    }

    // Create an HTTP reference to the catalog index
    _searchClient = new SearchServiceClient(_azureSearchServiceName, new SearchCredentials(_azureSearchAdminKey));

    // Read Webvtt and Add Captions to Azure Search
    foreach (Webvtt w in webvtts)
    {
        await UploadCaptionsAsync(contentId, w, log);
    }
}

static async Task<string> GetWebvttText(string url)
{
    var client = new HttpClient();
    var response = new HttpResponseMessage();
    //need await, meaning it's async operation
    response = await client.GetAsync(url);
    string result = await response.Content.ReadAsStringAsync();
    return result;
}

static async Task UploadCaptionsAsync(string contentId, Webvtt vtt, TraceWriter log)
{
    // Target Index
    string azureSearchIndex = string.Format("caption-{0}",vtt.lang);

    // Create IndexClient instance
    ISearchIndexClient _indexClient = _searchClient.Indexes.GetClient(azureSearchIndex);

    // Get vtt text from Url
    Task<string> vtttextTaskResult = GetWebvttText(vtt.webvtt_url);
    string vtttext = await vtttextTaskResult;

    List<IndexAction> indexOperations = GetCaptions( contentId, vtttext);
    try
    {
        if (indexOperations.Count > 0)
        {
            _indexClient.Documents.Index(new IndexBatch(indexOperations));
        }
    }
    catch (IndexBatchException e)
    {
        // Sometimes when your Search service is under load, indexing will fail for some of the documents in
        // the batch. Depending on your application, you can take compensating actions like delaying and
        // retrying. For this simple demo, we just log the failed document keys and continue.
        log.Info(
            "Failed to index some of the documents: {0}",
                String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
    }
}

static List<IndexAction> GetCaptions(string contentId, string vtt)
{
    List<IndexAction> indexOperations = new List<IndexAction>();
    string[] lines = vtt.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
    int docIndex = 0;
    string begin_s = string.Empty; string end_s = string.Empty; 
    
    foreach (var l in lines)
    {
        if (l == "WEBVTT" || string.IsNullOrWhiteSpace(l))
        {
            continue;
        }

        if (l.IndexOf("-->") != -1)
        {
            //Timecode
            var tc = l.Replace("-->", "|").Split('|');
            if (tc.Length !=2 )
            {
                // Invalid timecode format, and skip
                continue;
            }
            begin_s = tc[0].Trim();
            end_s = tc[1].Trim();
        }
        else
        {
            if (string.IsNullOrEmpty(begin_s) || string.IsNullOrEmpty(end_s))
            {
                continue;
            }

            var doc = new Microsoft.Azure.Search.Models.Document();
            string docId = string.Format("{0}-{1}", contentId, docIndex.ToString());
            doc.Add("id", docId);
            doc.Add("content_id", contentId);
            doc.Add("begin_s", begin_s);
            doc.Add("end_s", end_s);
            doc.Add("caption", l);
            indexOperations.Add(IndexAction.Upload(doc));
            docIndex++;
            begin_s = string.Empty; end_s = string.Empty;
        }
    }
    return indexOperations;
}
