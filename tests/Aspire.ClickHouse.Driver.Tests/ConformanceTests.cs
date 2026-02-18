// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Components.ConformanceTests;
using ClickHouse.Driver.ADO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.ClickHouse.Driver.Tests;

public class ConformanceTests : ConformanceTests<ClickHouseDataSource, ClickHouseClientSettings>, IClassFixture<ClickHouseContainerFixture>
{
    private readonly ClickHouseContainerFixture _containerFixture;

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "ClickHouse.Driver";

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => RequiresFeatureAttribute.IsFeatureSupported(TestFeature.Docker);

    protected override string? ConfigurationSectionName => "Aspire:ClickHouse:Driver";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "ClickHouse": {
              "Driver": {
                "ConnectionString": "Host=localhost;Port=8123",
                "DisableHealthChecks": false,
                "DisableTracing": false,
                "DisableMetrics": false
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
    {
        ("""{"Aspire": { "ClickHouse":{ "Driver": { "DisableHealthChecks": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
        ("""{"Aspire": { "ClickHouse":{ "Driver": { "DisableTracing": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
        ("""{"Aspire": { "ClickHouse":{ "Driver": { "DisableMetrics": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
    };

    protected override string[] RequiredLogCategories => [
        "ClickHouse.Driver",
        "ClickHouse.Driver.Connection",
        "ClickHouse.Driver.Command",
    ];

    public ConformanceTests(ClickHouseContainerFixture containerFixture, ITestOutputHelper? output = null) : base(output)
    {
        _containerFixture = containerFixture;
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
    {
        var connectionString = GetConnectionString();

        configuration.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>(
                    CreateConfigKey("Aspire:ClickHouse:Driver", key, "ConnectionString"),
                    connectionString)
            ]);
    }

    private string GetConnectionString()
    {
        if (RequiresFeatureAttribute.IsFeatureSupported(TestFeature.Docker))
        {
            return _containerFixture.GetConnectionString();
        }
        return "Host=localhost;Port=8123;Username=default;Password=;Database=default";
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<ClickHouseClientSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddClickHouseDataSource("clickhouse", configure);
        }
        else
        {
            builder.AddKeyedClickHouseDataSource(key, configure);
        }
    }

    protected override void SetHealthCheck(ClickHouseClientSettings options, bool enabled)
    {
        options.DisableHealthChecks = !enabled;
    }

    protected override void SetTracing(ClickHouseClientSettings options, bool enabled)
    {
        options.DisableTracing = !enabled;
    }

    protected override void SetMetrics(ClickHouseClientSettings options, bool enabled)
    {
        options.DisableMetrics = !enabled;
    }

    protected override void TriggerActivity(ClickHouseDataSource service)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var client = service.GetClient();
        client.PingAsync(cancellationToken: cts.Token).GetAwaiter().GetResult();
    }
}
