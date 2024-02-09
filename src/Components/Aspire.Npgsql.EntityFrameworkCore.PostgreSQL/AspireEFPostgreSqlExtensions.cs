// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a PostgreSQL database context in an Aspire application.
/// </summary>
public static partial class AspireEFPostgreSqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL";
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
    /// Reads the configuration from "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:{typeof(TContext).Name}" config section, or "Aspire:Npgsql:EntityFrameworkCore:PostgreSQL" if former does not exist.
    /// </para>
    /// <para>
    /// The <see cref="DbContext.OnConfiguring" /> method can then be overridden to configure <see cref="DbContext" /> options.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlEntityFrameworkCorePostgreSQLSettings.ConnectionString"/> is not provided.</exception>
    public static void AddNpgsqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = GetDbContextSettings<TContext>(builder);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        builder.Services.AddDbContextPool<TContext>(ConfigureDbContext);

        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            // delay validating the ConnectionString until the DbContext is requested. This ensures an exception doesn't happen until a Logger is established.
            if (string.IsNullOrEmpty(settings.ConnectionString))
            {
                throw new InvalidOperationException($"ConnectionString is missing. It should be provided in 'ConnectionStrings:{connectionName}' or under the 'ConnectionString' key in '{DefaultConfigSectionName}' or '{DefaultConfigSectionName}:{typeof(TContext).Name}' configuration section.");
            }

            // We don't register a logger factory, because there is no need to: https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            dbContextOptionsBuilder.UseNpgsql(settings.ConnectionString, builder =>
            {
                // Resiliency:
                // 1. Connection resiliency automatically retries failed database commands: https://www.npgsql.org/efcore/misc/other.html#execution-strategy
                if (settings.Retry)
                {
                    builder.EnableRetryOnFailure();
                }
                // 2. "Scale proportionally: You want to ensure that you don't scale out a resource to a point where it will exhaust other associated resources."
                // The pooling is enabled by default, the min pool size is 0 by default: https://www.npgsql.org/doc/connection-string-parameters.html#pooling
                // There is nothing for us to set here.
                // 3. "Timeout: Places limit on the duration for which a caller can wait for a response."
                // The timeouts have default values, except of Internal Command Timeout, which we should ignore:
                // https://www.npgsql.org/doc/connection-string-parameters.html#timeouts-and-keepalive
                // There is nothing for us to set here.
            });
            configureDbContextOptions?.Invoke(dbContextOptionsBuilder);
        }
    }

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="DbContext"/> is not registered in DI.</exception>
    public static void EnrichNpgsqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = GetDbContextSettings<TContext>(builder);

        configureSettings?.Invoke(settings);

        ConfigureRetry();

        ConfigureInstrumentation<TContext>(builder, settings);

        void ConfigureRetry()
        {
            if (!settings.Retry)
            {
                return;
            }

            // Resolving DbContext<TContextService> will resolve DbContextOptions<TContextImplementation>.
            // We need to replace the DbContextOptions service descriptor to inject more logic. This won't be necessary once
            // Aspire targets .NET 9 as EF will respect the calls to services.ConfigureDbContext<TContext>(). c.f. https://github.com/dotnet/efcore/pull/32518

            var oldDbContextOptionsDescriptor = builder.Services.FirstOrDefault(sd => sd.ServiceType == typeof(DbContextOptions<TContext>));

            if (oldDbContextOptionsDescriptor is null)
            {
                throw new InvalidOperationException($"DbContext<{typeof(TContext).Name}> was not registered");
            }

            builder.Services.Remove(oldDbContextOptionsDescriptor);

            var dbContextOptionsDescriptor = new ServiceDescriptor(
                oldDbContextOptionsDescriptor.ServiceType,
                oldDbContextOptionsDescriptor.ServiceKey,
                factory: (sp, key) =>
                {
                    var dbContextOptions = oldDbContextOptionsDescriptor.ImplementationFactory?.Invoke(sp) as DbContextOptions<TContext>;

                    var optionsBuilder = dbContextOptions != null
                        ? new DbContextOptionsBuilder<TContext>(dbContextOptions)
                        : new DbContextOptionsBuilder<TContext>();

                    optionsBuilder.UseNpgsql(options => options.EnableRetryOnFailure());

                    return optionsBuilder.Options;
                },
                oldDbContextOptionsDescriptor.Lifetime
                );

            builder.Services.Add(dbContextOptionsDescriptor);
        }
    }

    private static void ConfigureInstrumentation<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(IHostApplicationBuilder builder, NpgsqlEntityFrameworkCorePostgreSQLSettings settings) where TContext : DbContext
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
                    // Npgsql already provides quality tracing (via the Npgsql.OpenTelemetry package).
                    // We don't need to enable it for EF via OpenTelemetry.Instrumentation.EntityFrameworkCore.
                    tracerProviderBuilder.AddNpgsql();

                    // defining exporters is outside of the scope of a Component
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

                    NpgsqlCommon.AddNpgsqlMetrics(meterProviderBuilder);
                });
        }
    }

    private static NpgsqlEntityFrameworkCorePostgreSQLSettings GetDbContextSettings<TContext>(IHostApplicationBuilder builder)
    {
        NpgsqlEntityFrameworkCorePostgreSQLSettings settings = new();
        var typeSpecificSectionName = $"{DefaultConfigSectionName}:{typeof(TContext).Name}";
        var typeSpecificConfigurationSection = builder.Configuration.GetSection(typeSpecificSectionName);
        if (typeSpecificConfigurationSection.Exists()) // https://github.com/dotnet/runtime/issues/91380
        {
            typeSpecificConfigurationSection.Bind(settings);
        }
        else
        {
            builder.Configuration.GetSection(DefaultConfigSectionName).Bind(settings);
        }

        return settings;
    }
}
