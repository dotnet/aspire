// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class AspireAzureEfCoreCosmosDBExtensionsTests
{
    private const string ConnectionString = "AccountEndpoint=https://fake-account.documents.azure.com:443/;AccountKey=<fake-key>;";

    [Fact]
    public void CanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmosConnection", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:Region", "westus"),
        ]);

        builder.AddCosmosDbContext<TestDbContext>("cosmosConnection", "databaseName", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseCosmos(ConnectionString, "databaseName", cosmosBuilder =>
            {
                cosmosBuilder.RequestTimeout(TimeSpan.FromSeconds(608));
            });
        });

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<CosmosOptionsExtension>();
        Assert.NotNull(extension);

        // Ensure the RequestTimeout from config size was respected
        Assert.Equal(TimeSpan.FromSeconds(608), extension.RequestTimeout);

        // Ensure the Region from the lambda was respected
        Assert.Equal("westus", extension.Region);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanConfigureRequestTimeout(bool useSettings)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmosConnection", ConnectionString),
        ]);
        if (!useSettings)
        {
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:RequestTimeout", "00:10:08"),
            ]);
        }

        builder.AddCosmosDbContext<TestDbContext>("cosmosConnection", "databaseName",
                configureDbContextOptions: optionsBuilder => optionsBuilder.UseCosmos(ConnectionString, "databaseName"),
                configureSettings: useSettings ? settings => settings.RequestTimeout = TimeSpan.FromSeconds(608) : null);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<CosmosOptionsExtension>();
        Assert.NotNull(extension);

        // Ensure the RequestTimeout was respected
        Assert.Equal(TimeSpan.FromSeconds(608), extension.RequestTimeout);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequestTimeoutFromBuilderWinsOverOthers(bool useSettings)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmosConnection", ConnectionString),
        ]);
        if (!useSettings)
        {
            builder.Configuration.AddInMemoryCollection([
                new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:RequestTimeout", "400"),
            ]);
        }

        builder.AddCosmosDbContext<TestDbContext>("cosmosConnection", "databaseName",
                configureDbContextOptions: optionsBuilder =>
                {
                    optionsBuilder.UseCosmos(ConnectionString, "databaseName", cosmosBuilder =>
                    {
                        cosmosBuilder.RequestTimeout(TimeSpan.FromSeconds(123));
                    });
                },
                configureSettings: useSettings ? settings => settings.RequestTimeout = TimeSpan.FromSeconds(300) : null);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<CosmosOptionsExtension>();
        Assert.NotNull(extension);

        // Ensure the RequestTimeout from builder was respected
        Assert.Equal(TimeSpan.FromSeconds(123), extension.RequestTimeout);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    /// <summary>
    /// Verifies that two different DbContexts can be registered with different connection strings.
    /// </summary>
    [Fact]
    public void CanHave2DbContexts()
    {
        const string connectionString2 = "AccountEndpoint=https://fake-account2.documents.azure.com:443/;AccountKey=<fake-key2>;";

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos2", connectionString2),
        ]);

        builder.AddCosmosDbContext<TestDbContext>("cosmos", "test");
        builder.AddCosmosDbContext<TestDbContext2>("cosmos2", "test2");

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var context2 = host.Services.GetRequiredService<TestDbContext2>();

        var actualConnectionString = context.Database.GetCosmosDatabaseId();
        Assert.Equal("test", actualConnectionString);

        actualConnectionString = context2.Database.GetCosmosDatabaseId();
        Assert.Equal("test2", actualConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsWhenDbContextIsRegisteredBeforeAspireComponent(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseCosmos(ConnectionString, "databaseName"));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseCosmos(ConnectionString, "databaseName"));
        }

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCosmosDbContext<TestDbContext>("cosmos", "databaseName"));
        Assert.Equal("DbContext<TestDbContext> is already registered. Please ensure 'services.AddDbContext<TestDbContext>()' is not used when calling 'AddCosmosDbContext()' or use the corresponding 'Enrich' method.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesntThrowWhenDbContextIsRegisteredBeforeAspireComponentProduction(bool useServiceType)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos", ConnectionString)
        ]);

        if (useServiceType)
        {
            builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(options => options.UseCosmos(ConnectionString, "databaseName"));
        }
        else
        {
            builder.Services.AddDbContextPool<TestDbContext>(options => options.UseCosmos(ConnectionString, "databaseName"));
        }

        var exception = Record.Exception(() => builder.AddCosmosDbContext<TestDbContext>("cosmos", "databaseName"));

        Assert.Null(exception);
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

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;Database=db;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;Database=db;Container=mycontainer;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;DisableServerCertificateValidation=True", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;Container=mycontainer", "https://localhost:8081/")]
    [InlineData("https://example1.documents.azure.com:443", "https://example1.documents.azure.com/")]
    public void AddAzureCosmosClient_EnsuresConnectionStringIsCorrect(string connectionString, string expectedEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddCosmosDbContext<TestDbContext>("cosmos", "databaseName");

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var client = context.Database.GetCosmosClient();

        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
    }

    [Fact]
    public void AddCosmosDbContext_SetsDatabaseWhenPresentInConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var databaseName = "testdb";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database={databaseName};");

        EntityFrameworkCoreCosmosSettings? capturedSettings = null;
        builder.AddCosmosDbContext<TestDbContext>("cosmos",
            configureSettings: settings => capturedSettings = settings);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var client = context.Database.GetCosmosClient();

        Assert.NotNull(client);
        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
        Assert.NotNull(context.Database);
        Assert.Equal(databaseName, context.Database.GetCosmosDatabaseId());
        Assert.NotNull(capturedSettings);
        Assert.Equal(databaseName, capturedSettings.DatabaseName);
    }

    [Fact]
    public void AddCosmosDbContext_WithDatabaseName_FavorsOverNameInConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var databaseName = "testdb";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database=connectionStringDatabaseName;");

        builder.AddCosmosDbContext<TestDbContext>("cosmos", databaseName);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var client = context.Database.GetCosmosClient();

        Assert.NotNull(client);
        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
        Assert.NotNull(context.Database);
        Assert.Equal(databaseName, context.Database.GetCosmosDatabaseId());
    }

    [Fact]
    public void AddCosmosDbContext_WithDatabaseNameInSettings_FavorsOverNameInConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var databaseName = "testdb";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database=connectionStringDatabaseName;");

        builder.AddCosmosDbContext<TestDbContext>("cosmos",
            configureSettings: settings => settings.DatabaseName = databaseName);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();
        var client = context.Database.GetCosmosClient();

        Assert.NotNull(client);
        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
        Assert.NotNull(context.Database);
        Assert.Equal(databaseName, context.Database.GetCosmosDatabaseId());
    }

    [Fact]
    public void AddCosmosDbContext_WithNoConnectionString_ThrowsException()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCosmosDbContext<TestDbContext>("cosmos"));

        Assert.Contains("A DbContext could not be configured with this AddCosmosDbContext overload.", exception.Message);
    }

    [Fact]
    public void AddCosmosDbContext_WithDatabaseName_WithNoConnectionString_ThrowsException()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.AddCosmosDbContext<TestDbContext>("cosmos", "testdb");

        using var host = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);

        Assert.Contains("A DbContext could not be configured.", exception.Message);
    }

    [Fact]
    public void AddCosmosDbContext_ThrowWhenDatabaseNotInConnectionString()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddCosmosDbContext<TestDbContext>("cosmos"));
        Assert.Contains("A DbContext could not be configured with this AddCosmosDbContext overload.", exception.Message);
    }

    [Fact]
    public void AddAzureCosmosClient_FailsWithError()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            AddAzureCosmosClient_EnsuresConnectionStringIsCorrect("this=isnt;a=valid;cosmos=connectionstring", string.Empty));

        Assert.Contains("missing", e.Message);
        Assert.Contains("AccountEndpoint", e.Message);
    }

    [Fact]
    public void AddCosmosDbContext_WithConnectionNameAndSettings_AppliesConnectionSpecificSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "testdb";
        var databaseName = "testdbname";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            [$"Aspire:Microsoft:EntityFrameworkCore:Cosmos:{connectionName}:RequestTimeout"] = "60",
            [$"Aspire:Microsoft:EntityFrameworkCore:Cosmos:{connectionName}:DisableTracing"] = "true"
        });

        EntityFrameworkCoreCosmosSettings? capturedSettings = null;
        builder.AddCosmosDbContext<TestDbContext>(connectionName, databaseName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.Equal(TimeSpan.Parse("60"), capturedSettings.RequestTimeout);
        Assert.True(capturedSettings.DisableTracing);
    }

    [Fact]
    public void AddCosmosDbContext_WithConnectionSpecificAndContextSpecificSettings_PrefersContextSpecific()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var connectionName = "testdb";
        var databaseName = "testdbname";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{connectionName}"] = ConnectionString,
            // Connection-specific settings
            [$"Aspire:Microsoft:EntityFrameworkCore:Cosmos:{connectionName}:RequestTimeout"] = "60",
            // Context-specific settings wins
            [$"Aspire:Microsoft:EntityFrameworkCore:Cosmos:TestDbContext:RequestTimeout"] = "120"
        });

        EntityFrameworkCoreCosmosSettings? capturedSettings = null;
        builder.AddCosmosDbContext<TestDbContext>(connectionName, databaseName, settings =>
        {
            capturedSettings = settings;
        });

        Assert.NotNull(capturedSettings);
        Assert.Equal(TimeSpan.Parse("120"), capturedSettings.RequestTimeout);
    }

    private static void PopulateConfiguration(ConfigurationManager configuration, string connectionString) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmos", connectionString)
        ]);
}
