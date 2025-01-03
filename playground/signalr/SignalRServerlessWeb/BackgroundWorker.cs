// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Azure.SignalR.Management;
using System.Text.Json.Serialization;

namespace SignalRServerlessWeb;

public class BackgroundWorker(ServiceManager serviceManager, IHttpClientFactory httpClientFactory) : BackgroundService
{
    private static string s_etag = string.Empty;
    private static int s_starCount;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var hubContext = await serviceManager.CreateHubContextAsync("myHubName", stoppingToken);

            var httpClient = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/azure/azure-signalr");
            request.Headers.UserAgent.ParseAdd("Serverless");
            request.Headers.Add("If-None-Match", s_etag);
            var response = await httpClient.SendAsync(request, stoppingToken);
            if (response.Headers.Contains("Etag"))
            {
                s_etag = response.Headers.GetValues("Etag").First();
            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = await response.Content.ReadFromJsonAsync<GitResult>(stoppingToken);
                if (result != null)
                {
                    s_starCount = result.StarCount;
                }
            }

            await hubContext.Clients.All.SendCoreAsync("newMessage", [$"Current star count of https://github.com/Azure/azure-signalr is: {s_starCount}"], stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private sealed class GitResult
    {
        [JsonPropertyName("stargazers_count")]
        public int StarCount { get; set; }
    }
}
