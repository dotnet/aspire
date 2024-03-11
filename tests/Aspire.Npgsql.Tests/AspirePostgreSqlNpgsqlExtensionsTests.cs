// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Aspire.Npgsql.Tests;

public class AspirePostgreSqlNpgsqlExtensionsTests : IClassFixture<PostgreSQLContainerFixture>
{
    private readonly PostgreSQLContainerFixture _containerFixture;
    private string ConnectionString => _containerFixture.GetConnectionString();

    public AspirePostgreSqlNpgsqlExtensionsTests(PostgreSQLContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        Console.WriteLine ($"ConnectionString: {ConnectionString}");
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

        // npsql does not include the password in the connection string,
        // unless `Persist Security Info=true`
        NpgsqlConnectionStringBuilder connStringBuilder = new(ConnectionString)
        {
            Password = null
        };
        Assert.Equal(connStringBuilder.ConnectionString, dataSource.ConnectionString);
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", "unused")
        ]);

        void SetConnectionString(NpgsqlSettings settings) => settings.ConnectionString = ConnectionString;
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

    [RequiresDockerTheory]
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

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CustomDataSourceBuilderIsExecuted(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var wasCalled = false;
        void configureDataSourceBuilder(NpgsqlDataSourceBuilder b) => wasCalled = true;

        if (useKeyed)
        {
            builder.AddKeyedNpgsqlDataSource("npgsql", configureDataSourceBuilder: configureDataSourceBuilder);
        }
        else
        {
            builder.AddNpgsqlDataSource("npgsql", configureDataSourceBuilder: configureDataSourceBuilder);
        }

        var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.True(wasCalled);
    }
}
