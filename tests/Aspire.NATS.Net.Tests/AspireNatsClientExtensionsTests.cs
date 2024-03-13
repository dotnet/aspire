// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using Xunit;

namespace Aspire.NATS.Net.Tests;

public class AspireNatsClientExtensionsTests
{
    private const string ConnectionString = "nats://apire-host:4222";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:nats", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedNatsClient("nats");
        }
        else
        {
            builder.AddNatsClient("nats");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<INatsConnection>("nats") :
            host.Services.GetRequiredService<INatsConnection>();

        Assert.Equal(ConnectionString, connection.Opts.Url);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:nats", "unused")
        ]);

        static void SetConnectionString(NatsClientSettings settings) => settings.ConnectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedNatsClient("nats", SetConnectionString);
        }
        else
        {
            builder.AddNatsClient("nats", SetConnectionString);
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<INatsConnection>("nats") :
            host.Services.GetRequiredService<INatsConnection>();

        Assert.Equal(ConnectionString, connection.Opts.Url);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", connection.Opts.Url);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "nats" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Nats:Client", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:nats", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedNatsClient("nats");
        }
        else
        {
            builder.AddNatsClient("nats");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<INatsConnection>("nats") :
            host.Services.GetRequiredService<INatsConnection>();

        Assert.Equal(ConnectionString, connection.Opts.Url);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", connection.Opts.Url);
    }
}
