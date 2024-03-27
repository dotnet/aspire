// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.Data.SqlClient;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.Data.SqlClient.Tests;

public class ConformanceTests : ConformanceTests<SqlConnection, MicrosoftDataSqlClientSettings>, IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _containerFixture;
    protected override bool CanConnectToServer => RequiresDockerTheoryAttribute.IsSupported;
    private string ConnectionString => RequiresDockerTheoryAttribute.IsSupported
                                        ? _containerFixture.GetConnectionString()
                                        : "Data Source=fake;Database=master";
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/031ed48714e16ba4a5b099b6e14647994a0b9c1b/src/OpenTelemetry.Instrumentation.SqlClient/Implementation/SqlActivitySourceHelper.cs#L31
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.SqlClient";

    // TODO
    protected override string[] RequiredLogCategories => Array.Empty<string>();

    protected override bool SupportsKeyedRegistrations => true;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "SqlServer": {
              "SqlClient": {
                "ConnectionString": "YOUR_CONNECTION_STRING",
                "HealthChecks": true,
                "Tracing": true
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Microsoft": { "Data" : { "SqlClient":{ "Tracing": 0}}}}}""", "Value is \"integer\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "Data" : { "SqlClient":{ "ConnectionString": "Con", "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\"")
        };

    public ConformanceTests(SqlServerContainerFixture containerFixture)
        => _containerFixture = containerFixture;

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(CreateConfigKey("Aspire:Microsoft:Data:SqlClient", key, "ConnectionString"),
                ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MicrosoftDataSqlClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddSqlServerClient("sqlconnection", configure);
        }
        else
        {
            builder.AddKeyedSqlServerClient(key, configure);
        }
    }

    protected override void SetHealthCheck(MicrosoftDataSqlClientSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(MicrosoftDataSqlClientSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(MicrosoftDataSqlClientSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(SqlConnection service)
    {
        service.Open();
        using var command = service.CreateCommand();
        command.CommandText = "SELECT 1;";
        command.ExecuteScalar();
    }

    [RequiresDockerFact]
    public void TracingEnablesTheRightActivitySource()
        => RemoteExecutor.Invoke(() => RunWithFixtureAsync(obj => obj.ActivitySourceTest(key: null))).Dispose();

    [RequiresDockerFact]
    public void TracingEnablesTheRightActivitySource_Keyed()
        => RemoteExecutor.Invoke(() => RunWithFixtureAsync(obj => obj.ActivitySourceTest(key: "key"))).Dispose();

    private static async Task RunWithFixtureAsync(Action<ConformanceTests> test)
    {
        await using var fixture = new SqlServerContainerFixture();
        await fixture.InitializeAsync();
        test(new ConformanceTests(fixture));
    }

}
