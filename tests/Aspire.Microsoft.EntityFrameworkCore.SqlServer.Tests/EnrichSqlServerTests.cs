// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Microsoft.Data.SqlClient.Tests;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class EnrichSqlServerTests : ConformanceTests
{
    public EnrichSqlServerTests(SqlServerContainerFixture containerFixture)
        : base(containerFixture)
    {
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseSqlServer(ConnectionString));
        builder.EnrichSqlServerDbContext<TestDbContext>(configure);
    }

    [Fact]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.EnrichSqlServerDbContext<TestDbContext>());
        Assert.Equal("DbContext<TestDbContext> was not registered. Ensure you have registered the DbContext in DI before calling EnrichSqlServerDbContext.", exception.Message);
    }

    [Fact]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseSqlServer(ConnectionString));

        builder.EnrichSqlServerDbContext<TestDbContext>();
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
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichSqlServerDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the command timeout was respected
        Assert.Equal(123, extension.CommandTimeout);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<SqlServerRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichWithConflictingCommandTimeoutThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder =>
            {
                builder.CommandTimeout(123);
            });
        });

        builder.EnrichSqlServerDbContext<TestDbContext>(settings => settings.CommandTimeout = 456);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.Equal("Conflicting values for 'CommandTimeout' were found in MicrosoftEntityFrameworkCoreSqlServerSettings and set in DbContextOptions<TestDbContext>.", exception.Message);
    }

    [Fact]
    public void EnrichEnablesRetryByDefault()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichSqlServerDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<SqlServerRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(new WorkaroundToReadProtectedField(context).MaxRetryCount, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichPreservesDefaultWhenMaxRetryCountNotSet()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableRetry", "true")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder =>
            {
                builder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichSqlServerDbContext<TestDbContext>();

        // The service descriptor of DbContextOptions<TestDbContext> should not be affected since Retry is false
        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Same(oldOptionsDescriptor, optionsDescriptor);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to the configured value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<SqlServerRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(456, retryStrategy.MaxRetryCount);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichDoesntOverridesCustomRetry()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:SqlServer:DisableRetry", "false")
        ]);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder =>
            {
                builder.EnableRetryOnFailure(456);
            });
        });

        var oldOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(oldOptionsDescriptor);

        builder.EnrichSqlServerDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        var retryStrategy = Assert.IsType<SqlServerRetryingExecutionStrategy>(executionStrategy);
        Assert.Equal(456, retryStrategy.MaxRetryCount);
#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichSupportServiceType()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        });

        builder.EnrichSqlServerDbContext<TestDbContext>();

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }

    [Fact]
    public void EnrichSupportCustomOptionsLifetime()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContext<ITestDbContext, TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }, contextLifetime: ServiceLifetime.Singleton);

        builder.EnrichSqlServerDbContext<TestDbContext>();

        var optionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.NotNull(optionsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, optionsDescriptor.Lifetime);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<ITestDbContext>() as TestDbContext;
        Assert.NotNull(context);
    }

    [Fact]
    public void EnrichWithoutRetryPreservesCustomExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichSqlServerDbContext<TestDbContext>(settings => settings.DisableRetry = true);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        Assert.IsType<CustomExecutionStrategy>(executionStrategy);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }

    [Fact]
    public void EnrichWithRetryAndCustomExecutionStrategyThrows()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomExecutionStrategy(c)));
        });

        builder.EnrichSqlServerDbContext<TestDbContext>(settings => settings.DisableRetry = false);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<TestDbContext>);
        Assert.Equal("MicrosoftEntityFrameworkCoreSqlServerSettings.DisableRetry needs to be set when a custom Execution Strategy is configured.", exception.Message);
    }

    [Fact]
    public void EnrichWithRetryAndCustomRetryExecutionStrategy()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Services.AddDbContextPool<TestDbContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlServer(ConnectionString, builder => builder.ExecutionStrategy(c => new CustomRetryExecutionStrategy(c)));
        });

        builder.EnrichSqlServerDbContext<TestDbContext>(settings => settings.DisableRetry = false);

        using var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<SqlServerOptionsExtension>();
        Assert.NotNull(extension);

        // ensure the retry strategy is enabled and set to its default value
        Assert.NotNull(extension.ExecutionStrategyFactory);
        var executionStrategy = extension.ExecutionStrategyFactory(new ExecutionStrategyDependencies(new CurrentDbContext(context), context.Options, null!));
        Assert.IsType<CustomRetryExecutionStrategy>(executionStrategy);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }
}
