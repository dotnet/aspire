// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

public class ConformanceTests_Pooling : ConformanceTests<TestDbContext, PomeloEntityFrameworkCoreMySqlSettings>
{
    // in the future it can become a static property that reads the value from Env Var
    protected const string ConnectionString = "Server=localhost;User ID=root;Password=pass;Database=test";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/mysql-net/MySqlConnector/blob/6a63fa49795b54318085938e4a09cda0bc0ab2cd/src/MySqlConnector/Utilities/ActivitySourceHelper.cs#L61
    protected override string ActivitySourceName => "MySqlConnector";

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
        "Microsoft.EntityFrameworkCore.Migrations",
        "MySqlConnector.ConnectionPool",
        "MySqlConnector.MySqlBulkCopy",
        "MySqlConnector.MySqlCommand",
        "MySqlConnector.MySqlConnection",
        "MySqlConnector.MySqlDataSource",
    };

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "Pomelo": {
              "EntityFrameworkCore": {
                "MySql": {
                  "ConnectionString": "YOUR_CONNECTION_STRING",
                  "HealthChecks": false,
                  "DbContextPooling": true,
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
            ("""{"Aspire": { "Pomelo": { "EntityFrameworkCore":{ "MySql": { "MaxRetryCount": "5"}}}}}""", "Value is \"string\" but should be \"integer\""),
            ("""{"Aspire": { "Pomelo": { "EntityFrameworkCore":{ "MySql": { "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Pomelo": { "EntityFrameworkCore":{ "MySql": { "ConnectionString": "", "DbContextPooling": "Yes"}}}}}""", "Value is \"string\" but should be \"boolean\"")
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[2]
        {
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ConnectionString", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:ServerVersion", "8.2.0-mysql")
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<PomeloEntityFrameworkCoreMySqlSettings>? configure = null, string? key = null)
        => builder.AddMySqlDbContext<TestDbContext>("mysql", configure);

    protected override void SetHealthCheck(PomeloEntityFrameworkCoreMySqlSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(PomeloEntityFrameworkCoreMySqlSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(PomeloEntityFrameworkCoreMySqlSettings options, bool enabled)
        => options.Metrics = enabled;

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

    private static bool GetCanConnect()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>().UseMySql(connectionString: ConnectionString, new MySqlServerVersion(new Version(8, 2, 0)));
        using TestDbContext dbContext = new(builder.Options);

        try
        {
            dbContext.Database.EnsureCreated();

            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
