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

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, MicrosoftEntityFrameworkCoreSqlServerSettings>
{
    protected const string ConnectionString = "Host=fake;Database=catalog";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/031ed48714e16ba4a5b099b6e14647994a0b9c1b/src/OpenTelemetry.Instrumentation.SqlClient/Implementation/SqlActivitySourceHelper.cs#L31
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.SqlClient";

    protected override string[] RequiredLogCategories => new string[]
    {
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.ChangeTracking",
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "Microsoft.EntityFrameworkCore.Query",
        "Microsoft.EntityFrameworkCore.Database.Transaction",
        "Microsoft.EntityFrameworkCore.Database.Connection",
        "Microsoft.EntityFrameworkCore.Model",
        "Microsoft.EntityFrameworkCore.Model.Validation",
        "Microsoft.EntityFrameworkCore.Update",
        "Microsoft.EntityFrameworkCore.Migrations"
    };

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Microsoft": {
              "EntityFrameworkCore": {
                "SqlServer": {
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
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "Retry": "5"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new ("Aspire:Microsoft:EntityFrameworkCore:SqlServer:ConnectionString", ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configure = null, string? key = null)
        => builder.AddSqlServerDbContext<TestDbContext>("sqlconnection", configure);

    protected override void SetHealthCheck(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to verify pooling without touching DB")]
    public void DbContextPoolingRegistersIDbContextPool()
    {
        using IHost host = CreateHostWithComponent();

        IDbContextPool<TestDbContext>? pool = host.Services.GetService<IDbContextPool<TestDbContext>>();

        Assert.NotNull(pool);
    }

    [Fact]
    public void DbContextCanBeAlwaysResolved()
    {
        using IHost host = CreateHostWithComponent();

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
