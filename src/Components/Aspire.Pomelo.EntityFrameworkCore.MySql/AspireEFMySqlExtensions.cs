// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a MySQL database context in an Aspire application.
/// </summary>
public static partial class AspireEFMySqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Pomelo:EntityFrameworkCore:MySql";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Enables db context pooling, corresponding health check, logging and telemetry.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>
    /// <para>
    /// Reads the configuration from "Aspire:Pomelo:EntityFrameworkCore:MySql:{typeof(TContext).Name}" config section, or "Aspire:Pomelo:EntityFrameworkCore:MySql" if former does not exist.
    /// </para>
    /// <para>
    /// The <see cref="DbContext.OnConfiguring" /> method can then be overridden to configure <see cref="DbContext" /> options.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="services"/> or <paramref name="builder"/> is null.</exception>
    public static IServiceCollection EnrichMySqlEntityFrameworkCore<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        Action<PomeloEntityFrameworkCoreMySqlSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        PomeloEntityFrameworkCoreMySqlSettings settings = new();
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

        configureSettings?.Invoke(settings);

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

        return services;
    }
}
