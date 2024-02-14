// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ContainerResourceBuilderTests
{
    [Fact]
    public void WithImageMutatesImageName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithImage("redis-stack");
        Assert.Equal("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
    }

    [Fact]
    public void WithImageTagMutatesImageTag()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithImageTag("7.2.4");
        Assert.Equal("7.2.4", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageRegistryMutatesImageRegistry()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithImageRegistry("myregistry.azurecr.io");
        Assert.Equal("myregistry.azurecr.io", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Registry);
    }

    [Fact]
    public void WithImageSHA256MutatesImageSHA256()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd");
        Assert.Equal("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().SHA256);
    }
}
