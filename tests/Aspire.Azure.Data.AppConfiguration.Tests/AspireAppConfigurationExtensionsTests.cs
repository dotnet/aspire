// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.AppConfiguration.Tests;

public class AspireAppConfigurationExtensionsTests
{
    private const string ConnectionString = "Endpoint=https://aspiretests.azconfig.io;Id=fake;Secret=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAppConfigurationClient("appconfig");
        }
        else
        {
            builder.AddAzureAppConfigurationClient("appconfig");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig") :
            host.Services.GetRequiredService<ConfigurationClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig", "unused")
        ]);

        var connectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedAzureAppConfigurationClient("appconfig", settings => settings.ConnectionString = connectionString);
        }
        else
        {
            builder.AddAzureAppConfigurationClient("appconfig", settings => settings.ConnectionString = connectionString);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig") :
            host.Services.GetRequiredService<ConfigurationClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "Aspire:Azure:Data:AppConfiguration:appconfig:ConnectionString" : "Aspire:Azure:Data:AppConfiguration:ConnectionString";
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig", ConnectionString),
            new KeyValuePair<string, string?>(key, "unused")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAppConfigurationClient("appconfig");
        }
        else
        {
            builder.AddAzureAppConfigurationClient("appconfig");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig") :
            host.Services.GetRequiredService<ConfigurationClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig2", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:appconfig3", ConnectionString)
        ]);

        builder.AddKeyedAzureAppConfigurationClient("appconfig1");
        builder.AddKeyedAzureAppConfigurationClient("appconfig2");
        builder.AddKeyedAzureAppConfigurationClient("appconfig3");

        using var host = builder.Build();

        // Verify all three keyed services were registered
        var client1 = host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig1");
        var client2 = host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig2");
        var client3 = host.Services.GetRequiredKeyedService<ConfigurationClient>("appconfig3");

        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotNull(client3);

        Assert.NotSame(client1, client2);
        Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);
    }
}