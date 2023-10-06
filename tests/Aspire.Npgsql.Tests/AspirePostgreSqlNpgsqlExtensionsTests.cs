// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Aspire.Npgsql.Tests;

public class AspirePostgreSqlNpgsqlExtensionsTests
{
    private const string ConnectionString = "Host=localhost;Database=test_aspire_npgsql";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedNpgsqlDataSource("npgsql");
        }
        else
        {
            builder.AddNpgsqlDataSource("npgsql");
        }

        var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Equal(ConnectionString, dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", "unused")
        ]);

        static void SetConnectionString(NpgsqlSettings settings) => settings.ConnectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedNpgsqlDataSource("npgsql", SetConnectionString);
        }
        else
        {
            builder.AddNpgsqlDataSource("npgsql", SetConnectionString);
        }

        var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Equal(ConnectionString, dataSource.ConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "npgsql" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Npgsql", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedNpgsqlDataSource("npgsql");
        }
        else
        {
            builder.AddNpgsqlDataSource("npgsql");
        }

        var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Equal(ConnectionString, dataSource.ConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }
}
