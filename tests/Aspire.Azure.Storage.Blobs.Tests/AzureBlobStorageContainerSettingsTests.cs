// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Blobs;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBlobStorageContainerSettingsTests
{
    private const string EmulatorConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1";

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
    [InlineData("Endpoint=\"https://example.blob.core.windows.net\";ContainerName=\"my-container\"")]
    public void ParseConnectionString_With_ServiceUri(string connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("https://example.blob.core.windows.net/", settings.ServiceUri?.ToString());
        Assert.Equal("my-container", settings.BlobContainerName);
    }

    [Theory]
    [InlineData($"{EmulatorConnectionString};ContainerName=my-container")]
    [InlineData($"{EmulatorConnectionString};ContainerName=\"my-container\"")]
    public void ParseConnectionString_With_ConnectionString(string connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Contains(EmulatorConnectionString, settings.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ContainerName", settings.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("my-container", settings.BlobContainerName);
        Assert.Null(settings.ServiceUri);
    }

    [Theory]
    [InlineData($"Endpoint=not-a-uri;ContainerName=my-container")]
    public void ParseConnectionString_With_NotAUri(string connectionString)
    {
        var settings = new AzureBlobStorageContainerSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.True(string.IsNullOrEmpty(settings.ConnectionString));
        Assert.Equal("my-container", settings.BlobContainerName);
        Assert.Null(settings.ServiceUri);
    }
}
