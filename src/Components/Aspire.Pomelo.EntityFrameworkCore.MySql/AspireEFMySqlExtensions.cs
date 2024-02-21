// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;
using OpenTelemetry.Metrics;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a MySQL database context in an Aspire application.
/// </summary>
public static partial class AspireEFMySqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Pomelo:EntityFrameworkCore:MySql";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Registers the given <see cref="DbContext" /> as a service in the services provided by the <paramref name="builder"/>.
    /// Enables db context pooling, retries, corresponding health check, logging and telemetry.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="DbContextOptions"/> for the context.</param>
    /// <remarks>
    /// <para>
    /// Reads the configuration from "Aspire:Pomelo:EntityFrameworkCore:MySql:{typeof(TContext).Name}" config section, or "Aspire:Pomelo:EntityFrameworkCore:MySql" if former does not exist.
    /// </para>
    /// <para>
    /// The <see cref="DbContext.OnConfiguring" /> method can then be overridden to configure <see cref="DbContext" /> options.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="PomeloEntityFrameworkCoreMySqlSettings.ConnectionString"/> is not provided.</exception>
    public static void AddMySqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, PomeloEntityFrameworkCoreMySqlSettings>(
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

        void ConfigureDbContext(IServiceProvider serviceProvider, DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            // use the legacy method of setting the ILoggerFactory because Pomelo EF Core doesn't use MySqlDataSource
            if (serviceProvider.GetService<ILoggerFactory>() is { } loggerFactory)
            {
                MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
            }

            var connectionString = settings.ConnectionString ?? string.Empty;

            ServerVersion serverVersion;
            if (settings.ServerVersion is null)
            {
                ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName, $"{DefaultConfigSectionName}:{typeof(TContext).Name}", isEfDesignTime: EF.IsDesignTime);
                serverVersion = ServerVersion.AutoDetect(connectionString);
            }
            else
            {
                serverVersion = ServerVersion.Parse(settings.ServerVersion);
            }

            var builder = dbContextOptionsBuilder.UseMySql(connectionString, serverVersion, builder =>
            {
                // delay validating the ConnectionString until the DbContext is configured. This ensures an exception doesn't happen until a Logger is established.
                ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName, $"{DefaultConfigSectionName}:{typeof(TContext).Name}", isEfDesignTime: EF.IsDesignTime);

                // Resiliency:
                // 1. Connection resiliency automatically retries failed database commands: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/wiki/Configuration-Options#enableretryonfailure
                if (settings.Retry)
                {
                    builder.EnableRetryOnFailure();
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
    public static void EnrichMySqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = builder.GetDbContextSettings<TContext, PomeloEntityFrameworkCoreMySqlSettings>(
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
#pragma warning disable EF1001 // Internal EF Core API usage.
                if (optionsBuilder.Options.FindExtension<MySqlOptionsExtension>() is not MySqlOptionsExtension extension
                   || extension.ServerVersion is not ServerVersion serverVersion)
                {
                    throw new InvalidOperationException($"A DbContextOptions<{typeof(TContext).Name}> was not found. Please ensure 'ServerVersion' was configured.");
                }
#pragma warning restore EF1001 // Internal EF Core API usage.

                optionsBuilder.UseMySql(serverVersion, options => options.EnableRetryOnFailure());
            });
        }
    }

    private static void ConfigureInstrumentation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(IHostApplicationBuilder builder, PomeloEntityFrameworkCoreMySqlSettings settings) where TContext : DbContext
    {
        if (settings.HealthChecks)
        {
            // calling MapHealthChecks is the responsibility of the app, not Component
            builder.TryAddHealthCheck(
                name: typeof(TContext).Name,
                static hcBuilder => hcBuilder.AddDbContextCheck<TContext>());
        }

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    // add tracing from the underlying MySqlConnector ADO.NET library
                    tracerProviderBuilder.AddSource("MySqlConnector");
                });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(meterProviderBuilder =>
                {
                    // Currently EF provides only Event Counters:
                    // https://learn.microsoft.com/ef/core/logging-events-diagnostics/event-counters?tabs=windows#counters-and-their-meaning
                    meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                    {
                        // The magic strings come from:
                        // https://github.com/dotnet/efcore/blob/a1cd4f45aa18314bc91d2b9ea1f71a3b7d5bf636/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                        eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore");
                    });

                    // add metrics from the underlying MySqlConnector ADO.NET library
                    meterProviderBuilder.AddMeter("MySqlConnector");
                });
        }
    }
}
