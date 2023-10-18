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

namespace Aspire.Azure.Data.Cosmos.EntityFrameworkCore.Tests;

public class ConformanceTests_Pooling : ConformanceTests<TestDbContext, AzureEntityFrameworkCoreCosmosDBSettings>
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/cb5b2193ef9cacc0b9ef699e085022577551bf85/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/Implementation/EntityFrameworkDiagnosticListener.cs#L38
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.EntityFrameworkCore";

    protected override string[] RequiredLogCategories => new string[]
    {
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.ChangeTracking",
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "Microsoft.EntityFrameworkCore.Query",
    };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[]
        {
            new KeyValuePair<string, string?>("Aspire.Azure.Data.Cosmos.EntityFrameworkCore:ConnectionString",
                "Host=fake;Database=catalog"),
            new KeyValuePair<string, string?>("Aspire.Azure.Data.Cosmos.EntityFrameworkCore:DatabaseName",
                "TestDatabase"),
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<AzureEntityFrameworkCoreCosmosDBSettings>? configure = null, string? key = null)
        => builder.AddCosmosDBEntityFrameworkDBContext<TestDbContext>("cosmosdb", configure);

    protected override void SetHealthCheck(AzureEntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetTracing(AzureEntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(AzureEntityFrameworkCoreCosmosDBSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override string JsonSchemaPath
        => "src/Components/Aspire.Azure.Data.Cosmos.EntityFrameworkCore/ConfigurationSchema.json";

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Azure": {
              "Data": {
                "Cosmos": {
                  "EntityFrameworkCore": {
                    "ConnectionString": "YOUR_CONNECTION_STRING",
                    "Tracing": true,
                    "Metrics": true
                  }
                }
              }
            }
          }
        }
        """;

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
