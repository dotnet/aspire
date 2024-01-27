// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Microsoft.EntityFrameworkCore.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Azure Cosmos DB
/// </summary>
public static class AspireAzureEFCoreCosmosDBExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:EntityFrameworkCore:Cosmos";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Configures the connection pooling, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing settings. It's invoked after the settings are read from the configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="services"/> or <paramref name="builder"/> is null.</exception>
    public static IServiceCollection EnrichCosmosDbEntityFrameworkCore<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new EntityFrameworkCoreCosmosDBSettings();
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
            services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation();
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (settings.Metrics)
        {
            services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    // https://github.com/dotnet/efcore/blob/main/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore");
                });
            });
        }

        return services;
    }
}
