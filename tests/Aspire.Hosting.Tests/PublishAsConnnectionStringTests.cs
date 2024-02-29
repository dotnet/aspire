// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using System.Text.Json.Nodes;
using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.Tests;
public class PublishAsConnnectionStringTests
{
    [Fact]
    public void PublishAsConnectionStringConfiguresManifestAsParameter()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("redis").PublishAsConnectionString();

        Assert.True(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var annotation));

        var manifest = GetManifest(annotation.Callback!);

        Assert.NotNull(manifest);
        Assert.Equal("parameter.v0", manifest?["type"]?.ToString());
        Assert.Equal("{redis.value}", manifest?["connectionString"]?.ToString());
        Assert.Equal("{redis.inputs.value}", manifest?["value"]?.ToString());
    }

    private static JsonNode GetManifest(Action<ManifestPublishingContext> writeManifest)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        writeManifest(new ManifestPublishingContext(executionContext, Environment.CurrentDirectory, writer));
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        return obj;
    }
}
