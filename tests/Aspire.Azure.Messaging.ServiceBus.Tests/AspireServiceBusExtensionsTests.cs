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
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
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
            builder.AddKeyedAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();
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
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
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
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:sb2", "Endpoint=sb://aspireservicebustests2.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb3", "Endpoint=sb://aspireservicebustests3.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        builder.AddAzureServiceBusClient("sb1");
        builder.AddKeyedAzureServiceBusClient("sb2");
        builder.AddKeyedAzureServiceBusClient("sb3");

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<ServiceBusClient>();
        var client2 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb2");
        var client3 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb3");

        //Assert.NotSame(client1, client2);
        //Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);

        //Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client1.FullyQualifiedNamespace);
        Assert.Equal("aspireservicebustests2.servicebus.windows.net", client2.FullyQualifiedNamespace);
        Assert.Equal("aspireservicebustests3.servicebus.windows.net", client3.FullyQualifiedNamespace);
    }
}
