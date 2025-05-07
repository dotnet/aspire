// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Aspire.Azure.Storage.Queues;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageQueueSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(";")]
    [InlineData("Endpoint=https://example.queues.core.windows.net;")]
    [InlineData("QueueName=my-queue;")]
    [InlineData("Endpoint=https://example.queueName.core.windows.net;ExtraParam=value;")]
    public void ParseConnectionString_invalid_input(string? connectionString)
    {
        var settings = new AzureStorageQueueSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Null(settings.ConnectionString);
        Assert.Null(settings.QueueName);
    }

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
    public void ParseConnectionString_valid_input(string connectionString)
    {
        var settings = new AzureStorageQueueSettings();

        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);

        Assert.Equal("https://example.queueName.core.windows.net", settings.ConnectionString);
        Assert.Equal("my-queue", settings.QueueName);
    }
}
