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

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Oracle database 
/// </summary>
public static class AspireOracleEFCoreExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Oracle:EntityFrameworkCore";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Configures the connection pooling, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Oracle:EntityFrameworkCore:{typeof(TContext).Name}" config section, or "Aspire:Oracle:EntityFrameworkCore" if former does not exist.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="services"/> or <paramref name="builder"/> is null.</exception>
    public static IServiceCollection EnrichOracleEntityFrameworkCore<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        Action<OracleEntityFrameworkCoreSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        OracleEntityFrameworkCoreSettings settings = new();
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

        return services;
    }
}
