using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SignalR.Functions;

// Reference: https://github.com/aspnet/AzureSignalR-samples/tree/main/samples/QuickStartServerless/csharp-isolated
public class Functions
{
    private static readonly HttpClient s_httpClient = new();
    private static string s_etag = string.Empty;
    private static int s_starCount;

    [Function("index")]
    public static HttpResponseData GetWebPage([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString(File.ReadAllText("content/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }

    [Function("negotiate")]
    public static HttpResponseData Negotiate([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "serverless")] string connectionInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(connectionInfo);
        return response;
    }

    [Function("broadcast")]
    [SignalROutput(HubName = "serverless")]
    public static async Task<SignalRMessageAction> Broadcast([TimerTrigger("*/5 * * * * *")] TimerInfo timerInfo)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/azure/azure-signalr");
        request.Headers.UserAgent.ParseAdd("Serverless");
        request.Headers.Add("If-None-Match", s_etag);
        var response = await s_httpClient.SendAsync(request);
        if (response.Headers.Contains("Etag"))
        {
            s_etag = response.Headers.GetValues("Etag").First();
        }
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<GitResult>();
            if (result != null)
            {
                s_starCount = result.StarCount;
            }
        }
        return new SignalRMessageAction("newMessage", new object[] { $"Current star count of https://github.com/Azure/azure-signalr is: {s_starCount}" });
    }

    private sealed class GitResult
    {
        [JsonPropertyName("stargazers_count")]
        public int StarCount { get; set; }
    }
}
