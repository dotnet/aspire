// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class AspireEFPostgreSqlExtensionsTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=postgres";

    internal static void ConfigureDbContextOptionsBuilderForTesting(DbContextOptionsBuilder builder)
    {
        // Don't cache the service provider in testing.
        // Works around https://github.com/npgsql/efcore.pg/issues/2891, which is errantly caches connection strings across DI containers.
        builder.EnableServiceProviderCaching(false);
    }

    [Fact]
    public void ReadsFromConnectionStringsCorrectly()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        builder.AddNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        Assert.Equal(ConnectionString, context.Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void ConnectionStringCanBeSetInCode()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", "unused")
        ]);

        builder.AddNpgsqlDbContext<TestDbContext>("npgsql",
            settings => settings.ConnectionString = ConnectionString,
            configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

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
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:ConnectionString", "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        builder.AddNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: ConfigureDbContextOptionsBuilderForTesting);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", actualConnectionString);
    }

    [Fact]
    public void AddNpgsqlCanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Retry", "true")
        ]);

        builder.AddNpgsqlDbContext<TestDbContext>("npgsql", configureDbContextOptions: optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the connection string from config was respected
        var actualConnectionString = context.Database.GetDbConnection().ConnectionString;
        Assert.Equal(ConnectionString, actualConnectionString);

        // ensure the max retry count from config was respected
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichCanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Retry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the max retry count from config was respected
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichEnablesRetryByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the max retry count from config was respected
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichPreservesDefaultWhenMaxRetryCountNotSet()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Retry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        // The service descriptor should not be affected with MaxRetryCount set to 0
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Same(oldOptionsDescriptor, optionsDescriptor);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the max retry count from config was respected
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichOverridesCustomRetryIfNotDisabled()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Retry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the max retry count from config was respected
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<NpgsqlRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);

#pragma warning disable EF1001 // Internal EF Core API usage.
        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichSupportCustomOptionsLifetime()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContext<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.CommandTimeout(123);
            });
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);

#pragma warning disable EF1001 // Internal EF Core API usage.
        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    public class WorkaroundToReadProtectedField : NpgsqlRetryingExecutionStrategy
    {
        public WorkaroundToReadProtectedField(DbContext context) : base(context)
        {
        }

        public int RetryCount => base.MaxRetryCount;
    }

}
