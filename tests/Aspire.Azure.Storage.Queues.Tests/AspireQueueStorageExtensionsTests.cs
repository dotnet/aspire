// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Storage.Queues.Tests;

public class AspireQueueStorageExtensionsTests
{
    private const string ConnectionString = "AccountName=aspirestoragetests;AccountKey=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueService("queue");
        }
        else
        {
            builder.AddAzureQueueService("queue");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue", "AccountName=unused;AccountKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureQueueService("queue", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureQueueService("queue", settings => settings.ConnectionString = ConnectionString);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<QueueServiceClient>("queue") :
            host.Services.GetRequiredService<QueueServiceClient>();

        Assert.Equal("aspirestoragetests", client.AccountName);
    }
}
