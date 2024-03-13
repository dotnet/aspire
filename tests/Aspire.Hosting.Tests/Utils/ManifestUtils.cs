// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using System.Text.Json.Nodes;
using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.Utils;

internal sealed class ManifestUtils
{
    public static async Task<JsonNode> GetManifest(IResource resource)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        writer.WriteStartObject();
        await ManifestPublisher.WriteResourceAsync(resource, new ManifestPublishingContext(executionContext, Path.Combine(Environment.CurrentDirectory, "manifest.json"), writer));
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        var resourceNode = obj[resource.Name];
        Assert.NotNull(resourceNode);
        return resourceNode;
    }
}
