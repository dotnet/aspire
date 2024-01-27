// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, EntityFrameworkCoreCosmosDBSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/cb5b2193ef9cacc0b9ef699e085022577551bf85/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/Implementation/EntityFrameworkDiagnosticListener.cs#L38
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.EntityFrameworkCore";

    protected override string[] RequiredLogCategories => new string[]
    {
        "Microsoft.EntityFrameworkCore.ChangeTracking",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.Query",
    };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:ConnectionString",
                "Host=fake;Database=catalog"),
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<EntityFrameworkCoreCosmosDBSettings>? configure = null, string? key = null)
    {
        var connectionString = builder.Configuration.GetValue<string>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:ConnectionString");

        Assert.NotNull(connectionString);

        builder.Services.AddDbContextPool<TestDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseCosmos(connectionString, "TestDatabase"))
            .EnrichCosmosDbEntityFrameworkCore<TestDbContext>(builder, configure);
    }

    protected override void SetHealthCheck(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Microsoft": {
              "EntityFrameworkCore": {
                "Cosmos": {
                  "ConnectionString": "YOUR_CONNECTION_STRING",
                  "Tracing": true,
                  "Metrics": true
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
    {
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "Cosmos": { "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "Cosmos": { "Metrics": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("EF doesn't require a connection string");
    }

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();
    }
}
