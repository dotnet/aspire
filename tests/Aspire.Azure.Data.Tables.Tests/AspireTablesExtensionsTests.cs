// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.Tables.Tests;

public class AspireTablesExtensionsTests
{
    private const string ConnectionString = "AccountName=aspirestoragetests;AccountKey=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableService("tables");
        }
        else
        {
            builder.AddAzureTableService("tables");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", "AccountName=unused;AccountKey=myAccountKey")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableService("tables", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureTableService("tables", settings => settings.ConnectionString = ConnectionString);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "tables" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Data:Tables", key, "ServiceUri"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableService("tables");
        }
        else
        {
            builder.AddAzureTableService("tables");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServiceUriWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:tables", ConformanceTests.ServiceUri)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureTableService("tables");
        }
        else
        {
            builder.AddAzureTableService("tables");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<TableServiceClient>("tables") :
            host.Services.GetRequiredService<TableServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }
}
