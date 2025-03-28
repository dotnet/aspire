// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire;
using Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

#if NET9_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
#endif

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering a PostgreSQL database context in an Aspire application.
/// </summary>
public static partial class AspireAzureEFPostgreSqlExtensions
{
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
    public static void AddAzureNpgsqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
        this IHostApplicationBuilder builder,
        string connectionName,
        Action<AzureNpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null,
        Action<DbContextOptionsBuilder>? configureDbContextOptions = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureNpgsqlEntityFrameworkCorePostgreSQLSettings? azureSettings = null;

        builder.AddNpgsqlDbContext<TContext>(connectionName, settings => azureSettings = ConfigureSettings(configureSettings, settings), dbContextOptionsBuilder =>
        {
            Debug.Assert(azureSettings != null);

            ConfigureDbContextOptionsBuilder(azureSettings, dbContextOptionsBuilder);

            configureDbContextOptions?.Invoke(dbContextOptionsBuilder);
        });
    }

    /// <summary>
    /// Configures retries, health check, logging and telemetry for the <see cref="DbContext" />.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="DbContext"/> is not registered in DI.</exception>
    public static void EnrichAzureNpgsqlDbContext<[DynamicallyAccessedMembers(RequiredByEF)] TContext>(
            this IHostApplicationBuilder builder,
            Action<AzureNpgsqlEntityFrameworkCorePostgreSQLSettings>? configureSettings = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        AzureNpgsqlEntityFrameworkCorePostgreSQLSettings? azureSettings = null;

        builder.EnrichNpgsqlDbContext<TContext>(settings => azureSettings = ConfigureSettings(configureSettings, settings));

        // Enrich should always call ConfigureSettings
        Debug.Assert(azureSettings != null);

#if NET9_0_OR_GREATER
        builder.Services.ConfigureDbContext<TContext>(dbContextOptionsBuilder => ConfigureDbContextOptionsBuilder(azureSettings, dbContextOptionsBuilder));
#else
        builder.PatchServiceDescriptor<TContext>(dbContextOptionsBuilder =>
        {
            ConfigureDbContextOptionsBuilder(azureSettings, dbContextOptionsBuilder);
        });
#endif
    }

    private static AzureNpgsqlEntityFrameworkCorePostgreSQLSettings ConfigureSettings(Action<AzureNpgsqlEntityFrameworkCorePostgreSQLSettings>? userConfigureSettings, NpgsqlEntityFrameworkCorePostgreSQLSettings settings)
    {
        var azureSettings = new AzureNpgsqlEntityFrameworkCorePostgreSQLSettings();

        // Copy the values updated by Npgsql integration.
        CopySettings(settings, azureSettings);

        // Invoke the Aspire configuration.
        userConfigureSettings?.Invoke(azureSettings);

        // Copy to the Npgsql integration settings as it needs to get any values set in userConfigureSettings.
        CopySettings(azureSettings, settings);

        return azureSettings;
    }

    private static void CopySettings(NpgsqlEntityFrameworkCorePostgreSQLSettings source, NpgsqlEntityFrameworkCorePostgreSQLSettings destination)
    {
        destination.ConnectionString = source.ConnectionString;
        destination.DisableHealthChecks = source.DisableHealthChecks;
        destination.DisableMetrics = source.DisableMetrics;
        destination.DisableTracing = source.DisableTracing;
        destination.DisableRetry = source.DisableRetry;
        destination.CommandTimeout = source.CommandTimeout;
    }

    private static void ConfigureDbContextOptionsBuilder(AzureNpgsqlEntityFrameworkCorePostgreSQLSettings settings, DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        // The connection string requires the username to be provided. Since it will depend on the Managed Identity that is used
        // we attempt to get the username from the access token.

        var credential = settings.Credential ?? new DefaultAzureCredential();

#pragma warning disable EF1001 // Internal EF Core API usage.

        // Get the connection string from the Npgsql options extension in case it was set using UseNpgsql(connStr) and Enrich()
        var extensionsConnectionString = dbContextOptionsBuilder.Options.GetExtension<NpgsqlOptionsExtension>()?.ConnectionString;

#pragma warning restore EF1001 // Internal EF Core API usage.

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString ?? extensionsConnectionString);

        if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Username))
        {
            // Ensure to use the management scope, so the token contains user names for all managed identity types - e.g. user and service principal
            var token = credential.GetToken(ManagedIdentityTokenCredentialHelpers.ManagementTokenRequestContext, default);

            if (ManagedIdentityTokenCredentialHelpers.TryGetUsernameFromToken(token.Token, out var username))
            {
                dataSourceBuilder.ConnectionStringBuilder.Username = username;
            }
            else
            {
                // Otherwise check using the PostgresSql scope
                token = credential.GetToken(ManagedIdentityTokenCredentialHelpers.DatabaseForPostgresSqlTokenRequestContext, default);

                if (ManagedIdentityTokenCredentialHelpers.TryGetUsernameFromToken(token.Token, out username))
                {
                    dataSourceBuilder.ConnectionStringBuilder.Username = username;
                }
            }

            // If we still don't have a username, we let Npgsql handle the error when trying to connect.
        }

        if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Password))
        {
            dataSourceBuilder.UsePasswordProvider(
                passwordProvider: _ => credential.GetToken(ManagedIdentityTokenCredentialHelpers.DatabaseForPostgresSqlTokenRequestContext, default).Token,
                passwordProviderAsync: async (_, ct) => (await credential.GetTokenAsync(ManagedIdentityTokenCredentialHelpers.DatabaseForPostgresSqlTokenRequestContext, default).ConfigureAwait(false)).Token
            );

            dbContextOptionsBuilder.UseNpgsql(dataSourceBuilder.Build());
        }
    }
}
