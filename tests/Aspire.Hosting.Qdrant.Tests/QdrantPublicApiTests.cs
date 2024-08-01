// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Qdrant.Tests;

public class QdrantPublicApiTests
{
    [Fact]
    public void AddQdrantContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Qdrant";

        var action = () => builder.AddQdrant(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddQdrantContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddQdrant(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;
        const string source = "/qdrant/storage";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var qdrant = builderResource.AddQdrant("Qdrant");
        string source = null!;

        var action = () => qdrant.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<IResourceWithEnvironment> builder = null!;
        var builderResource = TestDistributedApplicationBuilder.Create();
        var qdrantResource = builderResource.AddQdrant("Qdrant");

        var action = () => builder.WithReference(qdrantResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenQdrantResourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var qdrant = builder.AddQdrant("Qdrant");
        IResourceBuilder<QdrantServerResource> qdrantResource = null!;

        var action = () => qdrant.WithReference(qdrantResource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(qdrantResource), exception.ParamName);
    }

    [Fact]
    public void CtorQdrantServerResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        string name = null!;
        const string key = nameof(key);
        var apiKey = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, key, special: false);

        var action = () => new QdrantServerResource(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorQdrantServerResourceShouldThrowWhenApiKeyIsNull()
    {
        const string name = "Qdrant";
        ParameterResource apiKey = null!;

        var action = () => new QdrantServerResource(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(apiKey), exception.ParamName);
    }
}
