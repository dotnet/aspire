// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using System.Text.Json.Nodes;
using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.Utils;

internal sealed class ManifestUtils
{
    public static async Task<JsonNode> GetManifest(IResource resource, string? manifestDirectory = null)
    {
        manifestDirectory ??= Environment.CurrentDirectory;

        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        writer.WriteStartObject();
        var context = new ManifestPublishingContext(executionContext, Path.Combine(manifestDirectory, "manifest.json"), writer);
        await context.WriteResourceAsync(resource);
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        var resourceNode = obj[resource.Name];
        Assert.NotNull(resourceNode);
        return resourceNode;
    }

    public static async Task<JsonNode[]> GetManifests(IResource[] resources)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var context = new ManifestPublishingContext(executionContext, Path.Combine(Environment.CurrentDirectory, "manifest.json"), writer);

        var results = new List<JsonNode>();

        foreach (var r in resources)
        {
            writer.WriteStartObject();
            await context.WriteResourceAsync(r);
            writer.WriteEndObject();
            writer.Flush();
            ms.Position = 0;
            var obj = JsonNode.Parse(ms);
            Assert.NotNull(obj);
            var resourceNode = obj[r.Name];
            Assert.NotNull(resourceNode);
            results.Add(resourceNode);

            ms.Position = 0;
            writer.Reset(ms);
        }

        return [.. results];
    }

    public static async Task<(JsonNode ManifestNode, string BicepText)> GetManifestWithBicep(IResource resource)
    {
        var manifestNode = await GetManifest(resource);

        if (!manifestNode.AsObject().TryGetPropertyValue("path", out var pathNode))
        {
            throw new ArgumentException("Specified resource does not contain a path property.", nameof(resource));
        }

        if (pathNode?.ToString() is not { } path || !File.Exists(path))
        {
            throw new ArgumentException("Path node in resource is null, empty, or does not exist.", nameof(resource));
        }

        var bicepText = await File.ReadAllTextAsync(path);
        return (manifestNode, bicepText);
    }
}
