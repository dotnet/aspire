// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Microsoft.EntityFrameworkCore.Cosmos;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
    /// Registers the given <see cref="DbContext" /> as a service in the services provided by the <paramref name="builder"/>.
    /// Configures the connection pooling, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext" /> that needs to be registered.</typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="databaseName">The name of the database to use within the Azure Cosmos DB account.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing settings. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDbContextOptions">An optional delegate to configure the <see cref="DbContextOptions"/> for the context.</param>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="EntityFrameworkCoreCosmosDBSettings.ConnectionString"/> is not provided.</exception>
    public static void AddCosmosDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        string databaseName,
        Action<EntityFrameworkCoreCosmosDBSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
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

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                settings.AccountEndpoint = uri;
            }
            else
            {
                settings.ConnectionString = connectionString;
            }
        }
        configureSettings?.Invoke(settings);

        if (settings.DbContextPooling)
        {
            builder.Services.AddDbContextPool<TContext>(ConfigureDbContext);
        }
        else
        {
            builder.Services.AddDbContext<TContext>(ConfigureDbContext);
        }

        if (settings.Tracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation();
                tracerProviderBuilder.AddSource("Azure.Cosmos.Operation");
            });
        }

        if (settings.Metrics)
        {
            builder.Services.AddOpenTelemetry().WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder.AddEventCountersInstrumentation(eventCountersInstrumentationOptions =>
                {
                    // https://github.com/dotnet/efcore/blob/main/src/EFCore/Infrastructure/EntityFrameworkEventSource.cs#L45
                    eventCountersInstrumentationOptions.AddEventSources("Microsoft.EntityFrameworkCore");
                });
            });
        }

        void ConfigureDbContext(DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            if (!string.IsNullOrEmpty(settings.ConnectionString))
            {
                dbContextOptionsBuilder.UseCosmos(settings.ConnectionString, databaseName, UseCosmosBody);
            }
            else if (settings.AccountEndpoint is not null)
            {
                var credential = settings.Credential ?? new DefaultAzureCredential();
                dbContextOptionsBuilder.UseCosmos(settings.AccountEndpoint.OriginalString, credential, databaseName, UseCosmosBody);
            }
            else
            {
                throw new InvalidOperationException(
                  $"A DbContext could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or either " +
                  $"{nameof(settings.ConnectionString)} or {nameof(settings.AccountEndpoint)} must be provided " +
                  $"in the '{DefaultConfigSectionName}' or '{typeSpecificSectionName}' configuration section.");
            }

            configureDbContextOptions?.Invoke(dbContextOptionsBuilder);
        }

        void UseCosmosBody(CosmosDbContextOptionsBuilder builder)
        {
            // We don't register logger factory, because there is no need to:
            // https://learn.microsoft.com/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.useloggerfactory?view=efcore-7.0#remarks
            if (settings.Region is not null)
            {
                builder.Region(settings.Region);
            }
        }
    }
}
