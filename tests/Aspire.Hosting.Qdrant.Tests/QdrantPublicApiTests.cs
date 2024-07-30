// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Qdrant.Tests;

public class QdrantPublicApiTests
{
    #region QdrantBuilderExtensions

    [Fact]
    public void AddQdrantContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Qdrant";

        var action = () => builder.AddQdrant(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddQdrantContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;

        var action = () => builder.AddQdrant(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<QdrantServerResource> builder = null!;
        const string source = "/qdrant/storage";

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        const string name = "Qdrant";
        var apiKeyParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, $"{name}-Key", special: false);
        var resource = new QdrantServerResource(name, apiKeyParameter);
        var builder = distributedApplicationBuilder.AddResource(resource);
        string source = null!;

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(source), exception.ParamName);
        });
    }

    [Fact]
    public void WithReferenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<IResourceWithEnvironment> builder = null!;
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        const string name = "Qdrant";
        var apiKeyParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, $"{name}-Key", special: false);
        var resource = new QdrantServerResource(name, apiKeyParameter);
        var qdrantResource = distributedApplicationBuilder.AddResource(resource);
        
        var action = () => builder.WithReference(qdrantResource);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithReferenceShouldThrowWhenQdrantResourceIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        const string name = "Qdrant";
        var apiKeyParameter = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, $"{name}-Key", special: false);
        var resource = new QdrantServerResource(name, apiKeyParameter);
        var builder = distributedApplicationBuilder.AddResource(resource);
        IResourceBuilder<QdrantServerResource> qdrantResource = null!;

        var action = () => builder.WithReference(qdrantResource);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(qdrantResource), exception.ParamName);
        });
    }

    #endregion

    #region QdrantServerResource

    [Fact]
    public void CtorQdrantServerResourceShouldThrowWhenNameIsNull()
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        string name = null!;
        const string key = nameof(key);
        var apiKey = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, key, special: false);

        var action = () => new QdrantServerResource(name, apiKey);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void CtorQdrantServerResourceShouldThrowWhenApiKeyIsNull()
    {
        const string name = "Qdrant";
        ParameterResource apiKey = null!;

        var action = () => new QdrantServerResource(name, apiKey);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(apiKey), exception.ParamName);
        });
    }

    #endregion
}
