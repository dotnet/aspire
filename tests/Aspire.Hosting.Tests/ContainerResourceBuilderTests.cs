// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common;
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
    public void WithImageMutatesImageNameAndTag()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis").WithImage("redis-stack", "1.0.0");
        Assert.Equal("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.Equal("1.0.0", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageAddsAnnotationIfNotExistingAndMutatesImageName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.RemoveAt(0);

        container.WithImage("new-image");
        Assert.Equal("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.Equal("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageMutatesImageNameOfLastAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.Add(new ContainerImageAnnotation { Image = "another-image" });

        container.WithImage("new-image");
        Assert.Equal("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Image);
        Assert.Equal("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Tag);
    }

    [Fact]
    public void WithImageTagMutatesImageTag()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis(ContainerImageTags.Redis.Image).WithImageTag(ContainerImageTags.Redis.Tag);
        Assert.Equal(ContainerImageTags.Redis.Tag, redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
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

    [Fact]
    public void WithImageTagThrowsIfNoImageAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageTag(ContainerImageTags.Redis.Tag));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [Fact]
    public void WithImageRegistryThrowsIfNoImageAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageRegistry("myregistry.azurecr.io"));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [Fact]
    public void WithImageSHA256ThrowsIfNoImageAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd"));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    private sealed class TestContainerResource(string name) : ContainerResource(name)
    {
    }
}
