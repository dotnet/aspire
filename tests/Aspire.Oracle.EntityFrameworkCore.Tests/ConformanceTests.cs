// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, OracleEntityFrameworkCoreSettings>
{
    protected const string ConnectionString = "Data Source=fake;";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => throw new NotImplementedException();

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
            "Oracle": {
              "EntityFrameworkCore": {
                "ConnectionString": "YOUR_CONNECTION_STRING",
                "HealthChecks": false
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {
            ("""{"Aspire": { "Oracle": { "EntityFrameworkCore":{ "Retry": "5"}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Oracle": { "EntityFrameworkCore":{ "HealthChecks": "false"}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new ("Aspire:Oracle:EntityFrameworkCore:ConnectionString", ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<OracleEntityFrameworkCoreSettings>? configure = null, string? key = null)
        => builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", configure);

    protected override void SetHealthCheck(OracleEntityFrameworkCoreSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(OracleEntityFrameworkCoreSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void SetMetrics(OracleEntityFrameworkCoreSettings options, bool enabled)
        => throw new NotImplementedException();

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    [Fact]
    public void DbContextPoolingRegistersIDbContextPool()
    {
        using IHost host = CreateHostWithComponent();

#pragma warning disable EF1001 // Internal EF Core API usage.
        IDbContextPool<TestDbContext>? pool = host.Services.GetService<IDbContextPool<TestDbContext>>();
#pragma warning restore EF1001

        Assert.NotNull(pool);
    }

    [Fact]
    public void DbContextCanBeAlwaysResolved()
    {
        using IHost host = CreateHostWithComponent();

        TestDbContext? dbContext = host.Services.GetService<TestDbContext>();

        Assert.NotNull(dbContext);
    }
}
