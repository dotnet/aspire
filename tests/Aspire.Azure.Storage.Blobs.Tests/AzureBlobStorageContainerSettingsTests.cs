// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBlobStorageContainerSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(";")]
    [InlineData("Endpoint=https://example.blob.core.windows.net;")]
    [InlineData("ContainerName=my-container;")]
    [InlineData("Endpoint=https://example.blob.core.windows.net;ExtraParam=value;")]
    public void ParseConnectionString_invalid_input(string? connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Null(settings.ConnectionString);
        Assert.Null(settings.BlobContainerName);
    }

    [Fact]
    public void ParseConnectionString_invalid_input_results_in_AE()
    {
        var settings = new AzureBlobStorageContainerSettings();
        string connectionString = "InvalidConnectionString";

        Assert.Throws<ArgumentException>(() => ((IConnectionStringSettings)settings).ParseConnectionString(connectionString));
    }

    [Theory]
    [InlineData("Endpoint=https://example.blob.core.windows.net;ContainerName=my-container")]
    [InlineData("Endpoint=https://example.blob.core.windows.net;ContainerName=my-container;ExtraParam=value")]
    [InlineData("endpoint=https://example.blob.core.windows.net;containername=my-container")]
    [InlineData("ENDPOINT=https://example.blob.core.windows.net;CONTAINERNAME=my-container")]
    public void ParseConnectionString_valid_input(string connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("https://example.blob.core.windows.net", settings.ConnectionString);
        Assert.Equal("my-container", settings.BlobContainerName);
    }
    
    [Fact]
    public void ParseConnectionString_with_quoted_endpoint()
    {
        // This test reproduces the issue where the connection string has quotes around the endpoint value
        var settings = new AzureBlobStorageContainerSettings();
        string connectionString = "Endpoint=\"https://example.blob.core.windows.net\";ContainerName=my-container";

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("https://example.blob.core.windows.net", settings.ConnectionString);
        Assert.Equal("my-container", settings.BlobContainerName);
    }

    [Fact]
    public void ParseConnectionString_with_emulator_format()
    {
        // Test with emulator connection string format where Endpoint value is itself a connection string
        var settings = new AzureBlobStorageContainerSettings();
        string connectionString = "Endpoint=\"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=key;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;\";ContainerName=foo-container;";

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=key;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;", settings.ConnectionString);
        Assert.Equal("foo-container", settings.BlobContainerName);
    }
}
