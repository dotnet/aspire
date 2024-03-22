// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.WebPubSub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.WebPubSub.Tests;

public class AspireWebPubSubExtensionsTests
{
    private const string ConnectionString = "Endpoint=https://aspirewebpubsubtests.webpubsub.azure.com/;AccessKey=fake;";
    private const string UnusedConnectionString = "Endpoint=https://unused.webpubsub.azure.com/;AccessKey=fake;";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:wps", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureWebPubSubHub("wps", "hub1");
        }
        else
        {
            builder.AddAzureWebPubSubHub("wps", "hub1");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<WebPubSubServiceClient>("wps") :
            host.Services.GetRequiredService<WebPubSubServiceClient>();

        Assert.Equal(ConformanceTests.Endpoint, client.Endpoint.AbsoluteUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:wps", UnusedConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureWebPubSubHub("wps", "hub1", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureWebPubSubHub("wps", "hub1", settings => settings.ConnectionString = ConnectionString);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<WebPubSubServiceClient>("wps") :
            host.Services.GetRequiredService<WebPubSubServiceClient>();

        Assert.Equal(ConformanceTests.Endpoint, client.Endpoint.AbsoluteUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "wps" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:WebPubSub", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:wps", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureWebPubSubHub("wps", "hub1");
        }
        else
        {
            builder.AddAzureWebPubSubHub("wps", "hub1");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<WebPubSubServiceClient>("wps") :
            host.Services.GetRequiredService<WebPubSubServiceClient>();

        Assert.Equal(ConformanceTests.Endpoint, client.Endpoint.AbsoluteUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:wps", ConformanceTests.Endpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureWebPubSubHub("wps", "hub1");
        }
        else
        {
            builder.AddAzureWebPubSubHub("wps", "hub1");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<WebPubSubServiceClient>("wps") :
            host.Services.GetRequiredService<WebPubSubServiceClient>();

        Assert.Equal(ConformanceTests.Endpoint, client.Endpoint.AbsoluteUri);
    }
}
