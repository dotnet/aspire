// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class ConformanceTests_Pooling : ConformanceTests<TestDbContext, EntityFrameworkCoreCosmosDBSettings>
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
        => builder.AddCosmosDbContext<TestDbContext>("cosmosdb", "TestDatabase", configure);

    protected override void SetHealthCheck(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(EntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override string JsonSchemaPath
        => "src/Components/Aspire.Microsoft.EntityFrameworkCore.Cosmos/ConfigurationSchema.json";

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
            ("""{"Aspire": { "Microsoft":{ "EntityFrameworkCore": { "Cosmos": { "AccountEndpoint": 3 }}}}}""", "Value is \"integer\" but should be \"string\""),
            ("""{"Aspire": { "Microsoft":{ "EntityFrameworkCore": { "Cosmos": { "AccountEndpoint": "hello" }}}}}""", "Value does not match format \"uri\""),
            ("""{"Aspire": { "Microsoft":{ "EntityFrameworkCore": { "Cosmos": { "Region": 3 }}}}}""", "Value is \"integer\" but should be \"string\""),
        };

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to verify pooling without touching DB")]
    public void DbContextPoolingRegistersIDbContextPool(bool enabled)
    {
        using IHost host = CreateHostWithComponent(options => options.DbContextPooling = enabled);

        IDbContextPool<TestDbContext>? pool = host.Services.GetService<IDbContextPool<TestDbContext>>();

        Assert.Equal(enabled, pool is not null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DbContextCanBeAlwaysResolved(bool enabled)
    {
        using IHost host = CreateHostWithComponent(options => options.DbContextPooling = enabled);

        TestDbContext? dbContext = host.Services.GetService<TestDbContext>();

        Assert.NotNull(dbContext);
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();
    }
}
