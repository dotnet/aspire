// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Aspire.Azure.EntityFrameworkCore.CosmosDB;

/// <summary>
/// Extension methods for configuring EntityFrameworkCore DbContext to Azure CosmosDB
/// </summary>
public static class AspireAzureEFCoreCosmosDBExtensions
{
    private const string DefaultConfigSectionName = "Aspire.Azure.EntityFrameworkCore.CosmosDB";
    private const DynamicallyAccessedMemberTypes RequiredByEF = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Registers the given <see cref="DbContext" /> as a service in the services provided by the <paramref name="builder"/>.
    /// Configures the connection pooling, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configure">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configurationSectionName">The key of the configuration section. If not provided the default is 'Aspire.Azure.EntityFrameworkCore.CosmosDB'</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if optional <paramref name="configurationSectionName"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureEntityFrameworkCoreCosmosDBSettings.ConnectionString"/> is not provided.</exception>
    public static void AddCosmosDBEntityFrameworkDBContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        Action<AzureEntityFrameworkCoreCosmosDBSettings>? configure = null,
        string configurationSectionName = DefaultConfigSectionName) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationSectionName);

        AzureEntityFrameworkCoreCosmosDBSettings configurationOptions = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(configurationOptions);

        if(string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            configurationOptions.ConnectionString = builder.Configuration.GetConnectionString("Aspire.Azure.CosmosDB");
        }

        configure?.Invoke(configurationOptions);

        if (configurationOptions.DbContextPooling)
        {
            builder.Services.AddDbContextPool<TContext>(ConfigureDbContext);
        }
        else
        {
            builder.Services.AddDbContext<TContext>(ConfigureDbContext);
        }

        if (configurationOptions.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation();
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (configurationOptions.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    // https://github.com/dotnet/efcore/blob/main/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore", "Azure-Cosmos-Operation-Request-Diagnostics");
                });
            });
        }

        if (configurationOptions.HealthChecks)
        {
            builder.Services.AddHealthChecks().AddDbContextCheck<TContext>();
        }

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
            {
                throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{configurationSectionName}' configuration section.");
            }

            if (string.IsNullOrEmpty(configurationOptions.DatabaseName))
            {
                throw new InvalidOperationException($"DatabaseName is missing. It should be provided under 'DatabaseName' key in '{configurationSectionName}' configuration section.");
            }

            // We don't register logger factory, because there is no need to:
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            dbContextOptionsBuilder.UseCosmos(configurationOptions.ConnectionString, configurationOptions.DatabaseName, builder =>
            {
            });
        }
    }
}
