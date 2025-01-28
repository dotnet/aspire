// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Milvus.Tests;

public class MilvusPublicApiTests
{
    [Fact]
    public void AddMilvusContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        var name = "Milvus";

        IResourceBuilder<ParameterResource> apiKey = null!;
        var action = () => builder.AddMilvus(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMilvusContainerShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var apiKey = builder.AddParameter("ApiKey", "root:Milvus");
        string name = null!;

        var action = () => builder.AddMilvus(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var apiKey = builder.AddParameter("ApiKey", "root:Milvus");

        var milvus = builder.AddMilvus("Milvus", apiKey);
        string name = null!;

        var action = () => milvus.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenSourceIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;
        string name = "db";

        var action = () => builder.AddDatabase(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithAttuShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;

        var action = () => builder.WithAttu();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;
        const string source = "/milvus/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var apiKey = builder.AddParameter("ApiKey", "root:Milvus");

        var milvus = builder.AddMilvus("Milvus", apiKey);
        string source = null!;

        var action = () => milvus.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithConfigurationBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;

        var action = () => builder.WithConfigurationBindMount("/milvus/configs/milvus.yaml");

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithConfigurationBindMountShouldThrowWhenConfigurationFilePathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var apiKey = builder.AddParameter("ApiKey", "root:Milvus");

        var milvus = builder.AddMilvus("Milvus", apiKey);
        string configurationFilePath = null!;
        var action = () => milvus.WithConfigurationBindMount(configurationFilePath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configurationFilePath), exception.ParamName);
    }

    [Fact]
    public void CtorMilvusServerResourceShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder([]);
        string name = null!;
        var apiKey = new ParameterResource("ApiKey", (pd) => "root:Milvus");

        var action = () => new MilvusServerResource(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorMilvusServerResourceShouldThrowWhenApiKeyIsNull()
    {
        var name = "Milvus";
        ParameterResource apiKey = null!;

        var action = () => new MilvusServerResource(name, apiKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(apiKey), exception.ParamName);
    }
}

