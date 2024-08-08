// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Polly;
using System.Text.Json;

namespace Aspire.Hosting.Redis;

internal sealed class RedisInsightConfigWriterHook(IHttpClientFactory httpClientFactory, ResourceNotificationService resourceNotificationService) : IDistributedApplicationLifecycleHook
{

    public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appModel);

        if (appModel.Resources.OfType<RedisInsightResource>().SingleOrDefault() is not { } redisInsightResource)
        {
            // No-op if there is no resource (removed after hook added).
            return;
        }

        var redisInstances = appModel.Resources.OfType<RedisResource>();

        if (!redisInstances.Any())
        {
            // No-op if there are no Redis resources present.
            return;
        }

        var connectionFileDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        if (!Directory.Exists(connectionFileDirectoryPath))
        {
            Directory.CreateDirectory(connectionFileDirectoryPath);
        }
        var connectionFilePath = Path.Combine(connectionFileDirectoryPath, "RedisInsight_connections.json");

        using (var stream = new FileStream(connectionFilePath, FileMode.OpenOrCreate)) {
            using var writer = new Utf8JsonWriter(stream);
            // Need to grant read access to the config file on unix like systems.
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(connectionFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            }

            writer.WriteStartArray();

            foreach (var redisResource in redisInstances)
            {
                if (redisResource.PrimaryEndpoint.IsAllocated)
                {
                    var endpoint = redisResource.PrimaryEndpoint;
                    writer.WriteStartObject();
                    writer.WriteString("host", endpoint.ContainerHost);
                    writer.WriteNumber("port", endpoint.Port);
                    writer.WriteString("name", redisResource.Name);
                    writer.WriteNumber("db", 0);
                    writer.WriteNull("username");
                    writer.WriteNull("password");
                    writer.WriteString("connectionType", "STANDALONE");
                    writer.WriteEndObject();
                }

            }

            writer.WriteEndArray();
        };
        
        await resourceNotificationService.WaitForResourceAsync(redisInsightResource.Name,KnownResourceStates.Running, cancellationToken).ConfigureAwait(false);

        var client = httpClientFactory.CreateClient();

        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(File.OpenRead(connectionFilePath));

        content.Add(fileContent, "file", "RedisInsight_connections.json");

        var insightEndpoint = redisInsightResource.PrimaryEndpoint;
        var apiUrl = $"{insightEndpoint.Scheme}://{insightEndpoint.Host}:{insightEndpoint.Port}/api/databases/import";

        var pipeline = new ResiliencePipelineBuilder().AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(2),
            MaxRetryAttempts = 5,
        }).Build();

        await pipeline.ExecuteAsync(async (ctx) =>
         {
             var response = await client.PostAsync(apiUrl, content, ctx)
             .ConfigureAwait(false);
             response.EnsureSuccessStatusCode();

         }, cancellationToken).ConfigureAwait(false);
    }
}
