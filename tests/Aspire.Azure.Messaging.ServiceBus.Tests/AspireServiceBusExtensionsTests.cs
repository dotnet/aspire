// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

public class AspireServiceBusExtensionsTests
{
    private const string ConnectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBus("sb");
        }
        else
        {
            builder.AddAzureServiceBus("sb");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", "Endpoint=sb://unused.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBus("sb", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureServiceBus("sb", settings => settings.ConnectionString = ConnectionString);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "sb" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBus("sb");
        }
        else
        {
            builder.AddAzureServiceBus("sb");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NamespaceWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConformanceTests.FullyQualifiedNamespace)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBus("sb");
        }
        else
        {
            builder.AddAzureServiceBus("sb");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }
}
