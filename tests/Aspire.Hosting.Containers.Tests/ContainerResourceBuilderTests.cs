// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Containers.Tests;

public class ContainerResourceBuilderTests
{
    [Fact]
    public void WithImageMutatesImageName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImage("redis-stack");
        Assert.Equal("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
    }

    [Fact]
    public void WithImageMutatesImageNameAndTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImage("redis-stack", "1.0.0");
        Assert.Equal("redis-stack", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.Equal("1.0.0", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageAddsAnnotationIfNotExistingAndMutatesImageName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.RemoveAt(0);

        container.WithImage("new-image");
        Assert.Equal("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Image);
        Assert.Equal("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageMutatesImageNameOfLastAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("app", "some-image");
        container.Resource.Annotations.Add(new ContainerImageAnnotation { Image = "another-image" });

        container.WithImage("new-image");
        Assert.Equal("new-image", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Image);
        Assert.Equal("latest", container.Resource.Annotations.OfType<ContainerImageAnnotation>().Last().Tag);
    }

    [Fact]
    public void WithImageTagMutatesImageTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageTag("7.1");
        Assert.Equal("7.1", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Tag);
    }

    [Fact]
    public void WithImageRegistryMutatesImageRegistry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageRegistry("myregistry.azurecr.io");
        Assert.Equal("myregistry.azurecr.io", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().Registry);
    }

    [Fact]
    public void WithImageSHA256MutatesImageSHA256()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder.AddContainer("redis", "redis").WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd");
        Assert.Equal("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd", redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single().SHA256);
    }

    [Fact]
    public void WithImageTagThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageTag("7.1"));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [Fact]
    public void WithImageRegistryThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageRegistry("myregistry.azurecr.io"));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [Fact]
    public void WithImageSHA256ThrowsIfNoImageAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        var exception = Assert.Throws<InvalidOperationException>(() => container.WithImageSHA256("42b5c726e719639fcc1e9dbc13dd843f567dcd37911d0e1abb9f47f2cc1c95cd"));
        Assert.Equal("The resource 'testcontainer' does not have a container image specified. Use WithImage to specify the container image and tag.", exception.Message);
    }

    [Theory]
    [InlineData("redis", "redis", "latest", null)]
    [InlineData("redis:latest", "redis", "latest", null)]
    [InlineData("registry.io/library/rabbitmq", "registry.io/library/rabbitmq", "latest", null)]
    [InlineData("postgres:tag", "postgres", "tag", null)]
    [InlineData("kafka@sha256:01234567890abcdef01234567890abcdef01234567890abcdef01234567890ab", "kafka", null, "01234567890abcdef01234567890abcdef01234567890abcdef01234567890ab")]
    [InlineData("registry.io/image:tag", "registry.io/image", "tag", null)]
    [InlineData("host.com/path/to/image@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "host.com/path/to/image", null, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    [InlineData("another.org/path/to/another/image:tag@sha256:9999999999999999999999999999999999999999999999999999999999999999", "another.org/path/to/another/image", null, "9999999999999999999999999999999999999999999999999999999999999999")]
    public void WithImageMutatesContainerImageAnnotation(string reference, string expectedImage, string? expectedTag, string? expectedSha256)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        container.WithImage(reference);

        AssertImageComponents(container, null, expectedImage, expectedTag, expectedSha256);
    }

    [Fact]
    public void WithImageThrowsWithConflictingTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        Assert.Throws<InvalidOperationException>(() => container.WithImage("image:tag", "anothertag"));
    }

    [Fact]
    public void WithImageThrowsWithConflictingTagAndDigest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddResource(new TestContainerResource("testcontainer"));

        Assert.Throws<ArgumentOutOfRangeException>(() => container.WithImage("image@sha246:abcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcdabcd", "tag"));
    }

    [Fact]
    public void WithImageOverridesExistingImageAndTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImage("yet-another-image:new-tag");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, null, "yet-another-image", "new-tag", null);
    }

    [Fact]
    public void WithImageOverridesExistingImageAndSha()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImage("yet-another-image@sha256:421c76d77563afa1914846b010bd164f395bd34c2102e5e99e0cb9cf173c1d87");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, null, "yet-another-image", null, "421c76d77563afa1914846b010bd164f395bd34c2102e5e99e0cb9cf173c1d87");
    }

    [Fact]
    public void WithImageWithoutRegistryShouldKeepExistingRegistryButOverwriteTagWithLatest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "image", "original-tag")
            .WithImageRegistry("foobar.io")
            .WithImage("different-image");

        var annotation = redis.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        AssertImageComponents(redis, "foobar.io", "different-image", "latest", null);
    }

    [Fact]
    public void WithImageWithoutTagShouldReplaceExistingTagWithLatest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("container", "redis-stack", "original-tag")
            .WithImage("redis-stack");

        AssertImageComponents(redis, null, "redis-stack", "latest", null);
    }

    [Fact]
    public void WithImageOverwritesSha256WithLatestTag()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("redis", "image")
            .WithImageSHA256("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
            .WithImage("redis-stack");

        AssertImageComponents(redis, null, "redis-stack", "latest", null);
    }

    [Fact]
    public void WithImagePullPolicyMutatesImagePullPolicyOfLastAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var redis = builder
            .AddContainer("redis", "image")
            .WithImagePullPolicy(ImagePullPolicy.Missing)
            .WithImagePullPolicy(ImagePullPolicy.Always);

        var annotation = redis.Resource.Annotations.OfType<ContainerImagePullPolicyAnnotation>().Single();

        Assert.Equal(ImagePullPolicy.Always, annotation.ImagePullPolicy);
    }

    private static void AssertImageComponents<T>(IResourceBuilder<T> builder, string? expectedRegistry, string expectedImage, string? expectedTag, string? expectedSha256)
        where T: IResource
    {
        var containerImage = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
        Assert.Multiple(() =>
        {
            Assert.Equal(expectedRegistry, containerImage.Registry);
            Assert.Equal(expectedImage, containerImage.Image);
            Assert.Equal(expectedTag, containerImage.Tag);
            Assert.Equal(expectedSha256, containerImage.SHA256);
        });
    }

    private sealed class TestContainerResource(string name) : ContainerResource(name)
    {
    }
}
