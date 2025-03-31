// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Templates.Tests;

public sealed class ProjectInfo
{
    [JsonIgnore]
    public HttpClient Client { get; set; } = default!;

    public EndpointInfo[] Endpoints { get; set; } = default!;

    /// <summary>
    /// Sends a GET request to the specified resource and returns the response message.
    /// </summary>
    public Task<HttpResponseMessage> HttpGetAsync(string bindingName, string path, CancellationToken cancellationToken = default)
    {
        var allocatedEndpoint = Endpoints.Single(e => e.Name == bindingName);
        var url = $"{allocatedEndpoint.Uri}{path}";

        return Client.GetAsync(url, cancellationToken);
    }

    /// <summary>
    /// Sends a GET request to the specified resource and returns the response body as a string.
    /// </summary>
    public Task<string> HttpGetStringAsync(string bindingName, string path, CancellationToken cancellationToken = default)
    {
        var allocatedEndpoint = Endpoints.Single(e => e.Name == bindingName);
        var url = $"{allocatedEndpoint.Uri}{path}";

        return Client.GetStringAsync(url, cancellationToken);
    }

    public async Task WaitForHealthyStatusAsync(string bindingName, ITestOutputHelper testOutput, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                var status = await HttpGetStringAsync(bindingName, "/health", cancellationToken);
                if (status == "Healthy")
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                testOutput.WriteLine($"WaitForHealthyStatusAsync failed for {bindingName}: {ex}");
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public static Dictionary<string, ProjectInfo> Parse(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, ProjectInfo>>(json)!;
}

public record EndpointInfo(string Name, string Uri);
