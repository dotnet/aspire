// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationBuilderExtensionsTests
{
    [Fact]
    public void CreateResourceBuilderByNameRequiresExistingResource()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var missingException = Assert.Throws<InvalidOperationException>(() => appBuilder.CreateResourceBuilder<RedisResource>("non-existent-resource"));
        Assert.Contains("not found", missingException.Message);
    }

    [Fact]
    public void CreateResourceBuilderByNameRequiresCompatibleType()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");
        var incorrectTypeException = Assert.Throws<InvalidOperationException>(() => appBuilder.CreateResourceBuilder<PostgresServerResource>("redis"));
        Assert.Contains("not assignable", incorrectTypeException.Message);
    }

    [Fact]
    public void CreateResourceBuilderByNameSupportsUpCast()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");

        // RedisResource implements ContainerResource, so this is acceptable.
        var newRedisBuilder = appBuilder.CreateResourceBuilder<ContainerResource>("redis");
        Assert.Same(originalRedis.Resource, newRedisBuilder.Resource);
    }

    [Fact]
    public void CreateResourceBuilderByReturnsSameResourceInstance()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var originalRedis = appBuilder.AddRedis("redis");
        var newRedisBuilder = appBuilder.CreateResourceBuilder<RedisResource>("redis");
        Assert.Same(originalRedis.Resource, newRedisBuilder.Resource);
    }

    [Fact]
    public void GroupBuilderAddsGroupAnnotationToAllResources()
    {
        var appBuilder = (DistributedApplicationBuilder)DistributedApplication.CreateBuilder();

        var group = appBuilder.CreateGroup("test-group");
        var redis1 = group.AddRedis("redis1");
        var redis2 = group.AddRedis("redis2");

        var container = appBuilder.AddContainer("test-container", "test-image");
        appBuilder.Build();

        Assert.Equal("test-group", Assert.Single(redis1.Resource.Annotations.OfType<ResourceGroupAnnotation>()).Name);
        Assert.Equal("test-group", Assert.Single(redis2.Resource.Annotations.OfType<ResourceGroupAnnotation>()).Name);
        Assert.Empty(container.Resource.Annotations.OfType<ResourceGroupAnnotation>());
    }

    [Fact]
    public void GroupBuilderBuildThrowsException()
    {
        var appBuilder = (DistributedApplicationBuilder)DistributedApplication.CreateBuilder();
        var group = appBuilder.CreateGroup("test-group");
        group.AddRedis("redis1");
        group.AddRedis("redis2");
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Throws<InvalidOperationException>(group.Build);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
