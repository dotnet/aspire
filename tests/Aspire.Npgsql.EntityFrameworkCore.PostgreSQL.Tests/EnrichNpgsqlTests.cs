// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class EnrichNpgsqlTests : ConformanceTests
{
    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(ConnectionString));
        builder.EnrichNpgsqlDbContext<TestDbContext>(configure);
    }

    [Fact]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichNpgsqlDbContext<TestDbContext>());
        Assert.Equal("DbContext<TestDbContext> was not registered. Ensure you have registered the DbContext in DI before calling EnrichNpgsqlDbContext.", exception.Message);
    }

    [Fact]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseNpgsql(ConnectionString));

        builder.EnrichNpgsqlDbContext<TestDbContext>();
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("Enrich doesn't use ConnectionString");
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
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

        // ensure the retry strategy is enabled and set to its default value
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
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

        // ensure the retry strategy is enabled and set to its default value
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString, npgsqlBuilder =>
            {
                npgsqlBuilder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        // The service descriptor of DbContextOptions<TestDbContext> should not be affected since Retry is false
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Same(oldOptionsDescriptor, optionsDescriptor);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<NpgsqlOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to the configured value
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
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

        // ensure the retry strategy is enabled and set to its default value
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
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        });

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }

    [Fact]
    public void EnrichSupportCustomOptionsLifetime()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContext<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            AspireEFPostgreSqlExtensionsTests.ConfigureDbContextOptionsBuilderForTesting(optionsBuilder);
            optionsBuilder.UseNpgsql(ConnectionString);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichNpgsqlDbContext<TestDbContext>();

        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);

        var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }
}
