#r "Newtonsoft.Json"
#r "Microsoft.Azure.Documents.Client"

using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

private static readonly string _cosmosDBAccountName = Environment.GetEnvironmentVariable("CosmosDBAccountName");
private static readonly string _cosmosDBAccountKey = Environment.GetEnvironmentVariable("CosmosDBAccountKey");
private static readonly string _textAnalyticsAPISubscriptionKey = Environment.GetEnvironmentVariable("TextAnalyticsAPISubscriptionKey");
private static readonly string _textAnalyticsAPILocation = Environment.GetEnvironmentVariable("TextAnalyticsAPILocation");
private static readonly int _textAnalyticsAPIMaxCharNum = 5000;

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
    if (data.ContentId == null)
        return new HttpResponseMessage(HttpStatusCode.BadRequest);

    string contentId = data.ContentId;
    
    try
    {
        MainAsync(contentId,outputDocument,log).Wait();
    }
    catch (DocumentClientException de)
    {
        Exception baseException = de.GetBaseException();
        log.Info(string.Format("{0} error occurred: {1}", de.StatusCode, de));
        return req.CreateResponse(HttpStatusCode.InternalServerError);
    }
    catch (Exception e)
    {
        log.Info(string.Format("Error: {0}", e));
        return req.CreateResponse(HttpStatusCode.InternalServerError);
    }

    return req.CreateResponse(HttpStatusCode.OK);
}

private static AzureRegions ParseAzureRegions( string value)
{
    switch( value )
    {
        case "westus":
            return AzureRegions.Westus;
        case "westeurope":
            return AzureRegions.Westeurope;
        case "southeastasia":
            return AzureRegions.Southeastasia;
        case "eastus2":
            return AzureRegions.Eastus2;
        case "westcentralus":
            return AzureRegions.Westcentralus;
    }
    return AzureRegions.Westus;
}

private static async Task MainAsync(string contentId, IAsyncCollector<object> outputDocument, TraceWriter log)
{
    // Create a client.
    ITextAnalyticsAPI client = new TextAnalyticsAPI();
    client.AzureRegion = ParseAzureRegions(_textAnalyticsAPILocation);
    client.SubscriptionKey = _textAnalyticsAPISubscriptionKey;

    DocumentClient dbclient = new DocumentClient(
        new Uri(string.Format("https://{0}.documents.azure.com:443/", _cosmosDBAccountName)), 
        _cosmosDBAccountKey
    );
    var response = await dbclient.ReadDocumentAsync(
                UriFactory.CreateDocumentUri("asset", "meta", contentId)
            );

    var meta = JsonConvert.DeserializeObject<Meta>(response.Resource.ToString());

    // Getting key-phrases
    string webvtt = meta.webvtt_url;
    Task<string> vtttextTaskResult = GetWebvttText(webvtt);
    string vtttext = await vtttextTaskResult;
    List<string> keyPhrases = new List<string>();
    // Getting CaptionText for key phrases extraction
    string captionText = ExtractCaptionText(vtttext);

    KeyPhraseBatchResult r = client.KeyPhrases(
        new MultiLanguageBatchInput(
            new List<MultiLanguageInput>()
            {
                new MultiLanguageInput("en","0",captionText)
            }
        )
    );
    foreach (var document in r.Documents)
    {
        foreach (string keyphrase in document.KeyPhrases)
        {
            keyPhrases.Add(keyphrase);
        }
    }
    log.Info(string.Format("KeyPhrases={0}", string.Join(", ", keyPhrases.ToArray())));

    // Update meta in CosmosDB using CosmosDB out Binding
    // Add KeyPhrases to meta
    meta.key_phrases=keyPhrases;
    await outputDocument.AddAsync(meta);

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

static string ExtractCaptionText(string vtt)
{
    string[] lines = vtt.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
    string begin_s = string.Empty; string end_s = string.Empty;
    string captionText = string.Empty;

    foreach (var l in lines)
    {
        if (l == "WEBVTT" || string.IsNullOrWhiteSpace(l))
        {
            continue;
        }
        if (l.IndexOf("-->") != -1)
        {
            //this is a timecode
            var tc = l.Replace("-->", "|").Split('|');
            if (tc.Length != 2)
            {
                // Invalid format, and skip
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
            captionText = captionText + l;
            begin_s = string.Empty; end_s = string.Empty;
        }
    }
    if (captionText.Length > _textAnalyticsAPIMaxCharNum)
    {
        captionText = captionText.Substring(0, _textAnalyticsAPIMaxCharNum);
    }
    return captionText;
}