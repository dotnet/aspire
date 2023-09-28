// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
    /// Configures the connection pooling, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="configure">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:EntityFrameworkCore:SqlServer:{typeof(TContext).Name}" config section, or "Aspire:Microsoft:EntityFrameworkCore:SqlServer" if former does not exist.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="MicrosoftEntityFrameworkCoreSqlServerSettings.ConnectionString"/> is not provided.</exception>
    public static void AddSqlServerDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configure = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        MicrosoftEntityFrameworkCoreSqlServerSettings configurationOptions = new();
        var typeSpecificSectionName = $"{DefaultConfigSectionName}:{typeof(TContext).Name}";
        var typeSpecificConfigurationSection = builder.Configuration.GetSection(typeSpecificSectionName);
        if (typeSpecificConfigurationSection.Exists()) // https://github.com/dotnet/runtime/issues/91380
        {
            typeSpecificConfigurationSection.Bind(configurationOptions);
        }
        else
        {
            builder.Configuration.GetSection(DefaultConfigSectionName).Bind(configurationOptions);
        }

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            configurationOptions.ConnectionString = builder.Configuration.GetConnectionString("Aspire.SqlServer");
        }

        configure?.Invoke(configurationOptions);

        if (string.IsNullOrEmpty(configurationOptions.ConnectionString))
        {
            throw new InvalidOperationException($"ConnectionString is missing. It should be provided under 'ConnectionString' key in '{DefaultConfigSectionName}' or '{typeSpecificSectionName}' configuration section.");
        }

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
            });
        }

        if (configurationOptions.Metrics)
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

        if (configurationOptions.HealthChecks)
        {
            builder.Services.AddHealthChecks().AddDbContextCheck<TContext>();
        }

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            // We don't register logger factory, because there is no need to:
            // https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            dbContextOptionsBuilder.UseSqlServer(configurationOptions.ConnectionString, builder =>
            {
                // Resiliency:
                // Connection resiliency automatically retries failed database commands
                if (configurationOptions.MaxRetryCount > 0)
                {
                    builder.EnableRetryOnFailure(configurationOptions.MaxRetryCount);
                }

                // The time in seconds to wait for the command to execute.
                if (configurationOptions.Timeout.HasValue)
                {
                    builder.CommandTimeout(configurationOptions.Timeout);
                }
            });
        }
    }
}
