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
using Npgsql;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, NpgsqlEntityFrameworkCorePostgreSQLSettings>
{
    // in the future it can become a static property that reads the value from Env Var
    protected const string ConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/npgsql/npgsql/blob/ef9db1ffe9e432c1562d855b46dfac3514726b1b/src/Npgsql.OpenTelemetry/TracerProviderBuilderExtensions.cs#L18
    protected override string ActivitySourceName => "Npgsql";

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

    // we don't want to have both EF and Npgsql loggers to be enabled
    protected override string[] NotAcceptableLogCategories => new string[]
    {
        "Npgsql.Connection",
        "Npgsql.Command",
        "Npgsql.Transaction",
        "Npgsql.Copy",
        "Npgsql.Replication",
        "Npgsql.Exception"
    };

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Npgsql": {
              "EntityFrameworkCore": {
                "PostgreSQL": {
                  "HealthChecks": false,
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
            ("""{"Aspire": { "Npgsql": { "EntityFrameworkCore":{ "PostgreSQL": { "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Npgsql": { "EntityFrameworkCore":{ "PostgreSQL": { "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Npgsql": { "EntityFrameworkCore":{ "PostgreSQL": { "Metrics": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString", ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configure = null, string? key = null)
    {
        var connectionString = builder.Configuration.GetValue<string>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString");
        builder.Services.AddDbContextPool<TestDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql(connectionString))
            .EnrichNpgsqlEntityFrameworkCore<TestDbContext>(builder, configure);
    }

    protected override void SetHealthCheck(NpgsqlEntityFrameworkCorePostgreSQLSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(NpgsqlEntityFrameworkCorePostgreSQLSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(NpgsqlEntityFrameworkCorePostgreSQLSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("Need to skip this test until https://github.com/npgsql/efcore.pg/issues/2891 is fixed.");
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();
    }

    private static bool GetCanConnect()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(connectionString: ConnectionString);
        using TestDbContext dbContext = new(builder.Options);

        try
        {
            dbContext.Database.EnsureCreated();

            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }
}
