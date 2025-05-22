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
    public void ParseConnectionString_null_or_empty_input(string? connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Null(settings.ConnectionString);
        Assert.Null(settings.BlobContainerName);
    }

    [Theory]
    [InlineData(";")]
    [InlineData("InvalidConnectionString")]
    [InlineData("Endpoint=")]
    [InlineData("Endpoint=https://example.blob.core.windows.net;")]
    [InlineData("ContainerName=my-container;")]
    [InlineData("Endpoint=https://example.blob.core.windows.net;ExtraParam=value;")]
    public void ParseConnectionString_invalid_input_throws(string connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        Assert.Throws<ArgumentException>(() => ((IConnectionStringSettings)settings).ParseConnectionString(connectionString));
    }

    // The "partial_input_handled_gracefully" test was removed because these cases
    // now throw exceptions in our stricter validation

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

    [Theory]
    [InlineData("https://example.blob.core.windows.net")]
    [InlineData("http://localhost:10000/devstoreaccount1")]
    public void ParseConnectionString_url_endpoint(string endpoint)
    {
        var settings = new AzureBlobStorageContainerSettings();

        // Using a URL directly as a connection string (deployed environment scenario)
        ((IConnectionStringSettings)settings).ParseConnectionString(endpoint);

        Assert.Equal(endpoint, settings.ConnectionString);
        Assert.Null(settings.BlobContainerName); // Container name should be provided separately
    }
}
