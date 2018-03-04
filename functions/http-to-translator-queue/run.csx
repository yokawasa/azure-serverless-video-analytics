#r "Newtonsoft.Json"
#r "System.Runtime.Serialization"

using System.Net;
using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, IAsyncCollector<string> outputQueueItem, TraceWriter log)
{
    log.Info($"HTTPTrigger Function was triggered!");

    string jsonContent = await req.Content.ReadAsStringAsync();
    log.Info("jsonContent : " + jsonContent);

    // Validate input objects
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    if (data["Azure Media Indexer 2 Preview"] == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass WebVTT AssetId in the input object" });
    log.Info("Input - Azure Media Indexer 2 Preview : " + data["Azure Media Indexer 2 Preview"]);
    if (data["Azure Media Indexer 2 Preview"].AssetId == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass WebVTT AssetId in the input object" });
    log.Info("Input - WebVTT AssetId : " + data["Azure Media Indexer 2 Preview"].AssetId);
    if (data.SourceLanguage == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass SourceLanguage in the input object" });
    log.Info("Input - SourceLanguage : " + data.SourceLanguage);
    if (data.TranslatedLanguages == null)
        return req.CreateResponse(HttpStatusCode.BadRequest, new { error = "Please pass TranslatedLanguages in the input object" });
    log.Info("Input - TranslatedLanguages : " + data.TranslatedLanguages);

    // Enqueue if input validation pass
    await outputQueueItem.AddAsync(jsonContent);

    return req.CreateResponse(HttpStatusCode.Accepted, "Accepted");
}
