// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
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

        using var host = builder.Build();
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

        using var host = builder.Build();
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

        using var host = builder.Build();
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
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableRetry", "false"),
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:CommandTimeout", "608")
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseOracle(orclBuilder =>
            {
                orclBuilder.MinBatchSize(123);
            });
        });

        using var host = builder.Build();
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
            new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:DisableRetry", "true"),
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseOracle(builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        using var host = builder.Build();
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanConfigureCommandTimeout(bool useSettings)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString),
        ]);
        if (!useSettings)
        {
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:CommandTimeout", "608")
            ]);
        }

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection",
                configureDbContextOptions: optionsBuilder => optionsBuilder.UseOracle(),
                configureSettings: useSettings ? settings => settings.CommandTimeout = 608 : null);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(608, extension.CommandTimeout);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CommandTimeoutFromBuilderWinsOverOthers(bool useSettings)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString),
        ]);
        if (!useSettings)
        {
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("Aspire:Oracle:EntityFrameworkCore:CommandTimeout", "400")
            ]);
        }

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection",
                configureDbContextOptions: optionsBuilder =>
                    optionsBuilder.UseOracle(builder => builder.CommandTimeout(123)),
                configureSettings: useSettings ? settings => settings.CommandTimeout = 300 : null);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<OracleOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout from builder was respected
        Assert.Equal(123, extension.CommandTimeout);

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

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var context2 = host.Services.GetRequiredService<TestDbContext2>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);

        actualConnectionString = context2.Database.GetDbConnection().ConnectionString;
        Assert.Equal(connectionString2, actualConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsWhenDbContextIsRegisteredBeforeAspireComponent(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseOracle(ConnectionString));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseOracle(ConnectionString));
        }

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection"));
        Assert.Equal("DbContext<TestDbContext> is already registered. Please ensure 'services.AddDbContext<TestDbContext>()' is not used when calling 'AddOracleDatabaseDbContext()' or use the corresponding 'Enrich' method.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesntThrowWhenDbContextIsRegisteredBeforeAspireComponentProduction(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseOracle(ConnectionString));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseOracle(ConnectionString));
        }

        var exception = Record.Exception(() => builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection"));

        Assert.Null(exception);
    }

    [Fact]
    public void CanPassNoInstrumentationSettingsToDbContext()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", o => o.InstrumentationOptions = null);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void CanPassSettingsToDbContext()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orclconnection", ConnectionString)
        ]);

        builder.AddOracleDatabaseDbContext<TestDbContext>("orclconnection", o => o.InstrumentationOptions = s => s.SetDbStatementForText = true);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void AddOracleDatabaseDbContext_WithConnectionNameAndSettings_AppliesConnectionSpecificSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "testdb";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            [$"Aspire:Oracle:EntityFrameworkCore:{connectionName}:CommandTimeout"] = "60",
            [$"Aspire:Oracle:EntityFrameworkCore:{connectionName}:DisableTracing"] = "true"
        });

        OracleEntityFrameworkCoreSettings? capturedSettings = null;
        builder.AddOracleDatabaseDbContext<TestDbContext>(connectionName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.Equal(60, capturedSettings.CommandTimeout);
        Assert.True(capturedSettings.DisableTracing);
    }

    [Fact]
    public void AddOracleDatabaseDbContext_WithConnectionSpecificAndContextSpecificSettings_PrefersContextSpecific()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "testdb";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            // Connection-specific settings
            [$"Aspire:Oracle:EntityFrameworkCore:{connectionName}:CommandTimeout"] = "60",
            // Context-specific settings wins
            [$"Aspire:Oracle:EntityFrameworkCore:TestDbContext:CommandTimeout"] = "120"
        });

        OracleEntityFrameworkCoreSettings? capturedSettings = null;
        builder.AddOracleDatabaseDbContext<TestDbContext>(connectionName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.Equal(120, capturedSettings.CommandTimeout);
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
