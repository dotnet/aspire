// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageQueueSettingsTests
{
    private const string EmulatorConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;QueueEndpoint=http://127.0.0.1:10000/devstoreaccount1";

    [Fact]
    public void ParseConnectionString_invalid_input_results_in_AE()
    {
        var settings = new AzureStorageQueueSettings();
        string connectionString = "InvalidConnectionString";

        Assert.Throws<ArgumentException>(() => ((IConnectionStringSettings)settings).ParseConnectionString(connectionString));
    }

    [Theory]
    [InlineData("Endpoint=https://example.queueName.core.windows.net;QueueName=my-queue")]
    [InlineData("Endpoint=https://example.queueName.core.windows.net;QueueName=my-queue;ExtraParam=value")]
    [InlineData("endpoint=https://example.queueName.core.windows.net;queuename=my-queue")]
    [InlineData("ENDPOINT=https://example.queueName.core.windows.net;QUEUENAME=my-queue")]
    [InlineData("Endpoint=\"https://example.queueName.core.windows.net\";QueueName=\"my-queue\"")]
    public void ParseConnectionString_With_ServiceUri(string connectionString)
    {
        var settings = new AzureStorageQueueSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("https://example.queuename.core.windows.net/", settings.ServiceUri?.ToString());
        Assert.Equal("my-queue", settings.QueueName);
    }

    [Theory]
    [InlineData($"{EmulatorConnectionString};QueueName=my-queue")]
    [InlineData($"{EmulatorConnectionString};QueueName=\"my-queue\"")]
    public void ParseConnectionString_With_ConnectionString(string connectionString)
    {
        var settings = new AzureStorageQueueSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Contains(EmulatorConnectionString, settings.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QueueName", settings.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("my-queue", settings.QueueName);
        Assert.Null(settings.ServiceUri);
    }

    [Theory]
    [InlineData($"Endpoint=not-a-uri;QueueName=my-queue")]
    public void ParseConnectionString_With_NotAUri(string connectionString)
    {
        var settings = new AzureStorageQueueSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.True(string.IsNullOrEmpty(settings.ConnectionString));
        Assert.Equal("my-queue", settings.QueueName);
        Assert.Null(settings.ServiceUri);
    }
}
