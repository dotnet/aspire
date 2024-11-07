// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public class CosmosPublicApiTests
{
    [Fact]
    public void AddAzureCosmosClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "cosmos";
        Action<MicrosoftAzureCosmosSettings>? configureSettings = null;
        Action< CosmosClientOptions >? configureClientOptions = null;

        var action = () => builder.AddAzureCosmosClient(connectionName, configureSettings, configureClientOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureCosmosClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;
        Action<MicrosoftAzureCosmosSettings>? configureSettings = null;
        Action<CosmosClientOptions>? configureClientOptions = null;

        var action = () => builder.AddAzureCosmosClient(connectionName, configureSettings, configureClientOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureCosmosClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "cosmos";
        Action<MicrosoftAzureCosmosSettings>? configureSettings = null;
        Action<CosmosClientOptions>? configureClientOptions = null;

        var action = () => builder.AddKeyedAzureCosmosClient(name, configureSettings, configureClientOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureCosmosClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;
        Action<MicrosoftAzureCosmosSettings>? configureSettings = null;
        Action<CosmosClientOptions>? configureClientOptions = null;

        var action = () => builder.AddKeyedAzureCosmosClient(name, configureSettings, configureClientOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
