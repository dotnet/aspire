// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Storage.Blobs.Tests;

public class AspireBlobStorageExtensionsTests
{
    private const string ConnectionString = "AccountName=aspirestoragetests;AccountKey=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:blob", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureBlobService("blob");
        }
        else
        {
            builder.AddAzureBlobService("blob");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<BlobServiceClient>("blob") :
            host.Services.GetRequiredService<BlobServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:blob", "AccountName=unused;AccountKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureBlobService("blob", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureBlobService("blob", settings => settings.ConnectionString = ConnectionString);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<BlobServiceClient>("blob") :
            host.Services.GetRequiredService<BlobServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "blob" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Storage:Blobs", key, "ServiceUri"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:blob", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureBlobService("blob");
        }
        else
        {
            builder.AddAzureBlobService("blob");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<BlobServiceClient>("blob") :
            host.Services.GetRequiredService<BlobServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServiceUriWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:blob", ConformanceTests.ServiceUri)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureBlobService("blob");
        }
        else
        {
            builder.AddAzureBlobService("blob");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<BlobServiceClient>("blob") :
            host.Services.GetRequiredService<BlobServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }
}
