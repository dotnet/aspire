// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure.Internal;
using Xunit;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public class AspireOracleEFCoreDatabaseExtensionsTests
{
    private const string ConnectionString = "Data Source=fake";

    [Fact]
    public void ReadsFromConnectionStringsCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection");

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Equal(ConnectionString, context.Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void ConnectionStringCanBeSetInCode()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", "unused")
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", settings => settings.ConnectionString = ConnectionString);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void ConnectionNameWinsOverConfigSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:ConnectionString", "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection");

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void CanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:Retry", "true"),
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:Timeout", "608")
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseOracle(orclBuilder =>
            {
                orclBuilder.MinBatchSize(123);
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the min batch size was respected
        Assert.Equal(123, extension.MinBatchSize);

        // ensure the connection string from config was respected
        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<OracleRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

        // ensure the command timeout from config was respected
        Assert.Equal(608, extension.CommandTimeout);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void CanConfigureDbContextOptionsWithoutRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:Retry", "false"),
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseOracle(builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the connection string from config was respected
        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);

        // ensure no retry strategy was registered
        Assert.Null(extension.ExecutionStrategyFactory);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    /// <summary>
    /// Verifies that two different DbContexts can be registered with different connection strings.
    /// </summary>
    [Fact]
    public void CanHave2DbContexts()
    {
        const string connectionString2 = "Data Source=fake2";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection2", connectionString2),
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection");
        builder.AddOracleDatabaseDbContext<TestDbContext2>("orclconnection2");

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var context2 = host.Services.GetRequiredService<TestDbContext2>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);

        actualConnectionString = context2.Database.GetDbConnection().ConnectionString;
        Assert.Equal(connectionString2, actualConnectionString);
    }

    public class TestDbContext2 : DbContext
    {
        public TestDbContext2(DbContextOptions<TestDbContext2> options) : base(options)
        {
        }

        public DbSet<Product> Products => Set<Product>();

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
        }
    }
}
