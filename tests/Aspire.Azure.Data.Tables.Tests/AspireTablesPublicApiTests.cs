// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core.Extensions;
using Azure.Data.Tables;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.Tables.Tests;

public class AspireTablesPublicApiTests
{
    [Fact]
    public void AddAzureTableClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "tables";
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureTableClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddAzureTableClientShouldThrowWhenConnectionNameIsNull()
    {
        var builder = new HostApplicationBuilder();
        string connectionName = null!;
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureTableClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddAzureTableClientShouldThrowWhenConnectionNameIsEmpty()
    {
        var builder = new HostApplicationBuilder();
        var connectionName = string.Empty;
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureTableClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureTableClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "tables";
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureTableClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureTableClientShouldThrowWhenConnectionNameIsNull()
    {
        var builder = new HostApplicationBuilder();
        string name = null!;
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureTableClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureTableClientShouldThrowWhenConnectionNameIsEmpty()
    {
        var builder = new HostApplicationBuilder();
        var name = string.Empty;
        Action<AzureDataTablesSettings>? configureSettings = null;
        Action<IAzureClientBuilder<TableServiceClient, TableClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureTableClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
