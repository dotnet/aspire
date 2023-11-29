// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace Aspire.Oracle.ManagedDataAccess.Core.Tests;

public class ConformanceTests : ConformanceTests<OracleConnection, OracleManagedDataAccessCoreSettings>
{
    private const string ConnectionSting = "user id=system;password=password;data source=localhost:port/freepdb1";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    protected override string ActivitySourceName => "Oracle.ManagedDataAccess.Core";

    protected override string[] RequiredLogCategories => [];

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string JsonSchemaPath => "src/Components/Aspire.Oracle.ManagedDataAccess.Core/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
            "Aspire": {
                "Oracle": {
                    "ManagedDataAccess":{
                        "Core": {
                            "ConnectionString": "YOUR_CONNECTION_STRING",
                            "HealthChecks": false,
                            "Tracing": true
                        }
                    }
                }
            }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Oracle": { "ManagedDataAccess": { "Core": { "Tracing": 0 }}}}}""", "Value is \"integer\" but should be \"boolean\""),
            ("""{"Aspire": { "Oracle": { "ManagedDataAccess": { "Core": { "ConnectionString": "Con", "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Oracle:ManagedDataAccess:Core", key, "ConnectionString"), ConnectionSting)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<OracleManagedDataAccessCoreSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddOracleManagedDataAccessCore("orcl", configure);
        }
        else
        {
            builder.AddKeyedOracleManagedDataAccessCore(key, configure);
        }
    }

    protected override void SetHealthCheck(OracleManagedDataAccessCoreSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(OracleManagedDataAccessCoreSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(OracleManagedDataAccessCoreSettings options, bool enabled) {}

    protected override void TriggerActivity(OracleConnection connection)
    {
        connection.Open();
        using OracleCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM DUAL";
        command.ExecuteScalar();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("key")]
    public void ConnectionCanBeResolved(string? key)
    {
        using IHost host = CreateHostWithComponent(key: key);

        OracleConnection? oracleConnection = Resolve<OracleConnection>();

        Assert.NotNull(oracleConnection);

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
        OracleConnection connection = new(ConnectionSting);
        OracleCommand? cmd = null;

        try
        {
            connection.Open();
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
