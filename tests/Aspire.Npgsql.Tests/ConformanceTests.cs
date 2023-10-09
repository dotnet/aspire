// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Aspire.Npgsql.Tests;

public class ConformanceTests : ConformanceTests<NpgsqlDataSource, NpgsqlSettings>
{
    private const string ConnectionSting = "Host=localhost;Database=test_aspire_npgsql;Username=postgres;Password=postgres";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/npgsql/npgsql/blob/ef9db1ffe9e432c1562d855b46dfac3514726b1b/src/Npgsql.OpenTelemetry/TracerProviderBuilderExtensions.cs#L18
    protected override string ActivitySourceName => "Npgsql";

    protected override string[] RequiredLogCategories => new string[]
    {
        "Npgsql.Connection",
        "Npgsql.Command",
        "Npgsql.Transaction",
        "Npgsql.Copy",
        "Npgsql.Replication",
        "Npgsql.Exception"
    };

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string JsonSchemaPath => "src/Components/Aspire.Npgsql/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "PostgreSql": {
              "Npgsql": {
                "ConnectionString": "YOUR_CONNECTION_STRING",
                "HealthChecks": false,
                "Tracing": true,
                "Metrics": true
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Npgsql":{ "Metrics": 0}}}""", "Value is \"integer\" but should be \"boolean\""),
            ("""{"Aspire": { "Npgsql":{ "ConnectionString": "Con", "HealthChecks": "false"}}}""", "Value is \"string\" but should be \"boolean\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Npgsql", key, "ConnectionString"), ConnectionSting)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddNpgsqlDataSource("npgsql", configure);
        }
        else
        {
            builder.AddKeyedNpgsqlDataSource(key, configure);
        }
    }

    protected override void SetHealthCheck(NpgsqlSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(NpgsqlSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(NpgsqlSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override void TriggerActivity(NpgsqlDataSource service)
    {
        using NpgsqlConnection connection = service.CreateConnection();
        connection.Open();
        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "Select 1;";
        command.ExecuteScalar();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("key")]
    public void BothDataSourceAndConnectionCanBeResolved(string? key)
    {
        using IHost host = CreateHostWithComponent(key: key);

        NpgsqlDataSource? npgsqlDataSource = Resolve<NpgsqlDataSource>();
        DbDataSource? dbDataSource = Resolve<DbDataSource>();
        NpgsqlConnection? npgsqlConnection = Resolve<NpgsqlConnection>();
        DbConnection? dbConnection = Resolve<DbConnection>();

        Assert.NotNull(npgsqlDataSource);
        Assert.Same(npgsqlDataSource, dbDataSource);

        Assert.NotNull(npgsqlConnection);
        Assert.NotNull(dbConnection);

        Assert.Equal(dbConnection.ConnectionString, npgsqlConnection.ConnectionString);
        Assert.Equal(npgsqlDataSource.ConnectionString, npgsqlConnection.ConnectionString);

        T? Resolve<T>() => key is null ? host.Services.GetService<T>() : host.Services.GetKeyedService<T>(key);
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource_Keyed()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: "key")).Dispose();
    }

    private static bool GetCanConnect()
    {
        NpgsqlConnection connection = new(ConnectionSting);
        NpgsqlCommand? cmd = null;

        try
        {
            string dbName = connection.Database;

            // postgres is the default administrative connection database of PostgreSQL
            // we need to switch to it before we create the test db
            connection = new(connection.ConnectionString.Replace(dbName, "postgres"));

            connection.Open();

            cmd = new NpgsqlCommand($"CREATE DATABASE {dbName}", connection);
            cmd.ExecuteNonQuery();
        }
        catch (PostgresException dbEx) when (dbEx.SqlState == "42P04")
        {
            return true; // db already exists
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            cmd?.Dispose();
            connection.Dispose();
        }

        return true;
    }
}
