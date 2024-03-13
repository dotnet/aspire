// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class PublishAsConnectionStringTests
{
    [Fact]
    public async Task PublishAsConnectionStringConfiguresManifestAsParameter()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddRedis("redis").PublishAsConnectionString();

        Assert.True(redis.Resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out _));

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        Assert.NotNull(manifest);
        Assert.Equal("parameter.v0", manifest?["type"]?.ToString());
        Assert.Equal("{redis.value}", manifest?["connectionString"]?.ToString());
        Assert.Equal("{redis.inputs.value}", manifest?["value"]?.ToString());
    }
}
