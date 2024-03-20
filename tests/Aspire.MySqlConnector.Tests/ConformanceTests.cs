// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Xunit;

namespace Aspire.MySqlConnector.Tests;

public class ConformanceTests : ConformanceTests<MySqlDataSource, MySqlConnectorSettings>, IClassFixture<MySqlContainerFixture>
{
    private readonly MySqlContainerFixture _containerFixture;
    private string ConnectionString => RequiresDockerTheoryAttribute.IsSupported
                                        ? _containerFixture.GetConnectionString()
                                        : "Server=localhost;User ID=root;Password=password;Database=test_aspire_mysql";
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/mysql-net/MySqlConnector/blob/d895afc013a5849d33a123a7061442e2cbb9ce76/src/MySqlConnector/Utilities/ActivitySourceHelper.cs#L61
    protected override string ActivitySourceName => "MySqlConnector";

    protected override string[] RequiredLogCategories => [
        "MySqlConnector.ConnectionPool",
        "MySqlConnector.MySqlBulkCopy",
        "MySqlConnector.MySqlCommand",
        "MySqlConnector.MySqlConnection",
        "MySqlConnector.MySqlDataSource",
    ];

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => RequiresDockerTheoryAttribute.IsSupported;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "MySqlConnector": {
              "ConnectionString": "YOUR_CONNECTION_STRING",
              "HealthChecks": false,
              "Tracing": true,
              "Metrics": true
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "MySqlConnector":{ "Metrics": 0}}}""", "Value is \"integer\" but should be \"boolean\""),
            ("""{"Aspire": { "MySqlConnector":{ "ConnectionString": "Con", "HealthChecks": "false"}}}""", "Value is \"string\" but should be \"boolean\"")
        };

    public ConformanceTests(MySqlContainerFixture containerFixture)
        => _containerFixture = containerFixture;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:MySqlConnector", key, "ConnectionString"), ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MySqlConnectorSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddMySqlDataSource("mysql", configure);
        }
        else
        {
            builder.AddKeyedMySqlDataSource(key, configure);
        }
    }

    protected override void SetHealthCheck(MySqlConnectorSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(MySqlConnectorSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(MySqlConnectorSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override void TriggerActivity(MySqlDataSource service)
    {
        using MySqlConnection connection = service.CreateConnection();
        connection.Open();
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = "Select 1;";
        command.ExecuteScalar();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("key")]
    public void BothDataSourceAndConnectionCanBeResolved(string? key)
    {
        using IHost host = CreateHostWithComponent(key: key);

        MySqlDataSource? mySqlDataSource = Resolve<MySqlDataSource>();
        DbDataSource? dbDataSource = Resolve<DbDataSource>();
        MySqlConnection? mySqlConnection = Resolve<MySqlConnection>();
        DbConnection? dbConnection = Resolve<DbConnection>();

        Assert.NotNull(mySqlDataSource);
        Assert.Same(mySqlDataSource, dbDataSource);

        Assert.NotNull(mySqlConnection);
        Assert.NotNull(dbConnection);

        Assert.Equal(dbConnection.ConnectionString, mySqlConnection.ConnectionString);
        Assert.Equal(mySqlDataSource.ConnectionString, mySqlConnection.ConnectionString);

        T? Resolve<T>() => key is null ? host.Services.GetService<T>() : host.Services.GetKeyedService<T>(key);
    }

    [RequiresDockerFact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => RunWithFixtureAsync(obj => obj.ActivitySourceTest(key: null))).Dispose();

    [RequiresDockerFact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => RunWithFixtureAsync(obj => obj.ActivitySourceTest(key: "key"))).Dispose();

    private static async Task RunWithFixtureAsync(Action<ConformanceTests> test)
    {
        await using var fixture = new MySqlContainerFixture();
        await fixture.InitializeAsync();
        test(new ConformanceTests(fixture));
    }
}
