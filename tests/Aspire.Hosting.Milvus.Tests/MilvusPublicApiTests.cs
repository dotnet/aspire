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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMilvusContainerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["Parameters:ApiKey"] = "root:Milvus";
        var apiKey = builder.AddParameter("ApiKey");
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddMilvus(name, apiKey);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDatabaseShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["Parameters:ApiKey"] = "root:Milvus";
        var apiKey = builder.AddParameter("ApiKey");

        var milvus = builder.AddMilvus("Milvus", apiKey);
        var name = isNull ? null! : string.Empty;

        var action = () => milvus.AddDatabase(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDatabaseShouldThrowWhenSourceIsNull()
    {
        IResourceBuilder<MilvusServerResource> builder = null!;
        var name = "db";

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["Parameters:ApiKey"] = "root:Milvus";
        var apiKey = builder.AddParameter("ApiKey");

        var milvus = builder.AddMilvus("Milvus", apiKey);
        var source = isNull ? null! : string.Empty;

        var action = () => milvus.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithConfigurationBindMountShouldThrowWhenConfigurationFilePathIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["Parameters:ApiKey"] = "root:Milvus";
        var apiKey = builder.AddParameter("ApiKey");
        var milvus = builder.AddMilvus("Milvus", apiKey);
        var configurationFilePath = isNull ? null! : string.Empty;

        var action = () => milvus.WithConfigurationBindMount(configurationFilePath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(configurationFilePath), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorMilvusServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var apiKey = new ParameterResource("ApiKey", (_) => "root:Milvus");

        var action = () => new MilvusServerResource(name, apiKey);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

