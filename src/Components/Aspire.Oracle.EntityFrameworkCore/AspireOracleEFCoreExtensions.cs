// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Oracle.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Oracle.EntityFrameworkCore;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Oracle database 
/// </summary>
public static class AspireOracleEFCoreExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Oracle:EntityFrameworkCore";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Registers the given <see cref="DbContext" /> as a service in the services provided by the <paramref name="builder"/>.
    /// Enables db context pooling, retries, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="DbContextOptions"/> for the context.</param>
    /// <remarks>Reads the configuration from "Aspire:Oracle:EntityFrameworkCore:{typeof(TContext).Name}" config section, or "Aspire:Oracle:EntityFrameworkCore" if former does not exist.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="OracleEntityFrameworkCoreSettings.ConnectionString"/> is not provided.</exception>
    public static void AddOracleDatabaseDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<OracleEntityFrameworkCoreSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, OracleEntityFrameworkCoreSettings>(
            DefaultConfigSectionName,
            (settings, section) => section.Bind(settings)
        );

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        builder.Services.AddDbContextPool<TContext>(ConfigureDbContext);

        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
		    ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName, $"{DefaultConfigSectionName}:{typeof(TContext).Name}", isEfDesignTime: EF.IsDesignTime);

            dbContextOptionsBuilder.UseOracle(settings.ConnectionString, builder =>
            {
                // Resiliency:
                // Connection resiliency automatically retries failed database commands
                if (settings.Retry)
                {
                    builder.ExecutionStrategy(context => new OracleRetryingExecutionStrategy(context));
                }

                // The time in seconds to wait for the command to execute.
                if (settings.Timeout.HasValue)
                {
                    builder.CommandTimeout(settings.Timeout);
                }
            });

            configureDbContextOptions?.Invoke(dbContextOptionsBuilder);
        }
    }

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="DbContext"/> is not registered in DI.</exception>
    public static void EnrichOracleDatabaseDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<OracleEntityFrameworkCoreSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, OracleEntityFrameworkCoreSettings>(
            DefaultConfigSectionName,
            (settings, section) => section.Bind(settings)
        );

        configureSettings?.Invoke(settings);

        ConfigureRetry();

        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureRetry()
        {
            if (!settings.Retry)
            {
                return;
            }

            builder.PatchServiceDescriptor<TContext>(optionsBuilder =>
                optionsBuilder.UseOracle(options => options.ExecutionStrategy(context => new OracleRetryingExecutionStrategy(context)))
            );
        }
    }

    private static void ConfigureInstrumentation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(IHostApplicationBuilder builder, OracleEntityFrameworkCoreSettings settings) where TContext : DbContext
    {
        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation();
            });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    // https://github.com/dotnet/efcore/blob/a1cd4f45aa18314bc91d2b9ea1f71a3b7d5bf636/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore");
                });
            });
        }

        if (settings.HealthChecks)
        {
            builder.TryAddHealthCheck(
                name: typeof(TContext).Name,
                static hcBuilder => hcBuilder.AddDbContextCheck<TContext>());
        }
    }
}
