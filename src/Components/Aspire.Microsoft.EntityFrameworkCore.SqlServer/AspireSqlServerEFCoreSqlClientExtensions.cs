// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Azure SQL, MS SQL server 
/// </summary>
public static class AspireSqlServerEFCoreSqlClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:EntityFrameworkCore:SqlServer";
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
    /// <remarks>Reads the configuration from "Aspire:Microsoft:EntityFrameworkCore:SqlServer:{typeof(TContext).Name}" config section, or "Aspire:Microsoft:EntityFrameworkCore:SqlServer" if former does not exist.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="MicrosoftEntityFrameworkCoreSqlServerSettings.ConnectionString"/> is not provided.</exception>
    public static void AddSqlServerDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, MicrosoftEntityFrameworkCoreSqlServerSettings>(
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
            // We don't register logger factory, because there is no need to:
            // https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            dbContextOptionsBuilder.UseSqlServer(settings.ConnectionString, builder =>
            {
                ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName, $"{DefaultConfigSectionName}:{typeof(TContext).Name}", isEfDesignTime: EF.IsDesignTime);

                // Resiliency:
                // Connection resiliency automatically retries failed database commands
                if (settings.Retry)
                {
                    builder.EnableRetryOnFailure();
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
    public static void EnrichSqlServerDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, MicrosoftEntityFrameworkCoreSqlServerSettings>(
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
            {
                optionsBuilder.UseSqlServer(options => options.EnableRetryOnFailure());
            });
        }
    }

    private static void ConfigureInstrumentation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(IHostApplicationBuilder builder, MicrosoftEntityFrameworkCoreSqlServerSettings settings) where TContext : DbContext
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
                    // https://github.com/dotnet/efcore/blob/main/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                    // https://github.com/dotnet/SqlClient/blob/main/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/SqlClientEventSource.cs#L73
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore", "Microsoft.Data.SqlClient.EventSource");
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
