// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.MongoDB.EntityFrameworkCore.Tests;

public class AspireMongoDBEntityFrameworkCoreExtensionsTests
{
    private const string ConnectionString = "mongodb://localhost:27017/test";
    private const string DatabaseName = "testdb";

    internal static void ConfigureDbContextOptionsBuilderForTesting(DbContextOptionsBuilder builder)
    {
        // Don't cache the service provider in testing.
        // Works around https://github.com/mongodb/efcore.pg/issues/2891, which is errantly caches connection strings across DI containers.
        builder.EnableServiceProviderCaching(false);
    }

    [Fact]
    public void ReadsFromConnectionStringsCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString),
            new KeyValuePair<string, string?>($"Aspire:MongoDB:EntityFrameworkCore:mongodb:DatabaseName", DatabaseName),

        ]);

        builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb","testdb", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var mongoExtension = context.Options.FindExtension<MongoOptionsExtension>();
        var actualConnectionString = mongoExtension?.ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
    }

    [Fact]
    public void ConnectionStringCanBeSetInCode()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", "unused"),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:mongodb:DatabaseName", "testdb")
        ]);

        builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb","testdb",
            settings => settings.ConnectionString = ConnectionString,
            configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var mongoExtension = context.Options.FindExtension<MongoOptionsExtension>();
        var actualConnectionString = mongoExtension?.ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void ConnectionNameWinsOverConfigSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:ConnectionString", "unused"),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:DatabaseName", "testdb"),
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString)
        ]);

        builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb", "testdb", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var mongoExtension = context.Options.FindExtension<MongoOptionsExtension>();
        var actualConnectionString = mongoExtension?.ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void AddMongoDBCanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:DatabaseName", "testdb")
        ]);

        builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb","testdb", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseMongoDB(ConnectionString, "testdb", _ => ConfigureDbContextOptionsBuilderForTesting(optionsBuilder));
        });

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<MongoOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the connection string from config was respected
        var mongoExtension = context.Options.FindExtension<MongoOptionsExtension>();
        var actualConnectionString = mongoExtension?.ConnectionString;
        var actualDatabaseName = mongoExtension?.DatabaseName;
        Assert.Equal(ConnectionString, actualConnectionString);
        Assert.Equal(DatabaseName, actualDatabaseName);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    /// <summary>
    /// Verifies that two different DbContexts can be registered with different connection strings.
    /// </summary>
    [Fact]
    public void CanHave2DbContexts()
    {
        const string connectionString2 = "mongodb://localhost:27017/test2";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:mongodb:DatabaseName", DatabaseName),
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb2", connectionString2),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:mongodb2:DatabaseName", DatabaseName),
        ]);

        builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb","testdb");
        builder.AddMongoDBDatabaseDbContext<TestDbContext2>("mongodb2","testdb");

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var context2 = host.Services.GetRequiredService<TestDbContext2>();

        var mongoExtension = context.Options.FindExtension<MongoOptionsExtension>();
        var actualConnectionString = mongoExtension?.ConnectionString;
        var actualDatabaseName = mongoExtension?.DatabaseName;
        Assert.Equal(ConnectionString, actualConnectionString);
        Assert.Equal(DatabaseName, actualDatabaseName);

        mongoExtension = context2.Options.FindExtension<MongoOptionsExtension>();
        actualConnectionString = mongoExtension?.ConnectionString;
        actualDatabaseName = mongoExtension?.DatabaseName;
        Assert.Equal(connectionString2, actualConnectionString);
        Assert.Equal(DatabaseName, actualDatabaseName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsWhenDbContextIsRegisteredBeforeAspireComponent(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseMongoDB(ConnectionString));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseMongoDB(ConnectionString));
        }

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb", "testdb"));
        Assert.Equal("DbContext<TestDbContext> is already registered. Please ensure 'services.AddDbContext<TestDbContext>()' is not used when calling 'AddMongoDBDatabaseDbContext()' or use the corresponding 'Enrich' method.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesntThrowWhenDbContextIsRegisteredBeforeAspireComponentProduction(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:mongodb", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:MongoDB:EntityFrameworkCore:mongodb:DatabaseName", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseMongoDB(ConnectionString));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseMongoDB(ConnectionString));
        }

        var exception = Record.Exception(() => builder.AddMongoDBDatabaseDbContext<TestDbContext>("mongodb", "testdb"));

        Assert.Null(exception);
    }

    [Fact]
    public void AddmongodbDbContext_WithConnectionNameAndSettings_AppliesConnectionSpecificSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "mongodb";
        var databaseName = "testdb";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            [$"ConnectionStrings:{connectionName}:DatabaseName"] = DatabaseName,
            [$"Aspire:MongoDB:EntityFrameworkCore:{connectionName}:DisableHealthChecks"] = "true",
            [$"Aspire:MongoDB:EntityFrameworkCore:{connectionName}:DisableTracing"] = "true"
        });

        builder.AddMongoDBDatabaseDbContext<TestDbContext>(connectionName, DatabaseName );

        MongoDBEntityFrameworkCoreSettings? capturedSettings = null;
        builder.AddMongoDBDatabaseDbContext<TestDbContext>(connectionName,databaseName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.True(capturedSettings.DisableTracing);
        Assert.True(capturedSettings.DisableHealthChecks);
    }

    [Fact]
    public void AddmongodbDbContext_WithConnectionSpecificAndContextSpecificSettings_PrefersContextSpecific()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "mongodb";
        var databaseName = "testdb";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            // Connection-specific settings
            [$"Aspire:MongoDB:EntityFrameworkCore:{connectionName}:DatabaseName"] = "testdb",
        });

        MongoDBEntityFrameworkCoreSettings? capturedSettings = null;
        builder.AddMongoDBDatabaseDbContext<TestDbContext>(connectionName,databaseName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
    }

    public class TestDbContext2(DbContextOptions<TestDbContext2> options) : DbContext(options)
    {
        public DbContextOptions<TestDbContext2> Options { get; } = options;

        public DbSet<Product> Products => Set<Product>();

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = default!;
        }
    }
}
