// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire;
using Aspire.Azure.Npgsql;
using Aspire.Npgsql;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to an Azure Database for PostgreSQL with Npgsql client
/// </summary>
public static class AspireAzureNpgsqlExtensions
{
    /// <summary>
    /// Registers <see cref="NpgsqlDataSource"/> service for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="NpgsqlDataSourceBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if mandatory <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided or the <see cref="AzureNpgsqlSettings.Credential"/> is invalid.</exception>
    public static void AddAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<AzureNpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureNpgsqlSettings? azureSettings = null;

        builder.AddNpgsqlDataSource(connectionName, settings => azureSettings = ConfigureSettings(configureSettings, settings), dataSourceBuilder =>
        {
            Debug.Assert(azureSettings != null);

            ConfigureDataSourceBuilder(azureSettings, dataSourceBuilder);

            configureDataSourceBuilder?.Invoke(dataSourceBuilder);
        });
    }

    /// <summary>
    /// Registers <see cref="NpgsqlDataSource"/> as a keyed service for given <paramref name="name"/> for connecting PostgreSQL database with Npgsql client.
    /// Configures health check, logging and telemetry for the Npgsql client.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <param name="configureDataSourceBuilder">An optional delegate that can be used for customizing the <see cref="NpgsqlDataSourceBuilder"/>.</param>
    /// <remarks>Reads the configuration from "Aspire:Npgsql:{name}" section.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if mandatory <paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="NpgsqlSettings.ConnectionString"/> is not provided or the <see cref="AzureNpgsqlSettings.Credential"/> is invalid.</exception>
    public static void AddKeyedAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<AzureNpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        AzureNpgsqlSettings? azureSettings = null;

        builder.AddKeyedNpgsqlDataSource(name, settings => azureSettings = ConfigureSettings(configureSettings, settings), dataSourceBuilder =>
        {
            Debug.Assert(azureSettings != null);

            ConfigureDataSourceBuilder(azureSettings, dataSourceBuilder);

            configureDataSourceBuilder?.Invoke(dataSourceBuilder);
        });
    }

    private static AzureNpgsqlSettings ConfigureSettings(Action<AzureNpgsqlSettings>? userConfigureSettings, NpgsqlSettings settings)
    {
        var azureSettings = new AzureNpgsqlSettings();

        // Copy the values updated by Npgsql integration.
        CopySettings(settings, azureSettings);

        // Invoke the Aspire configuration.
        userConfigureSettings?.Invoke(azureSettings);

        // Copy to the Npgsql integration settings as it needs to get any values set in userConfigureSettings.
        CopySettings(azureSettings, settings);

        return azureSettings;
    }

    private static void CopySettings(NpgsqlSettings source, NpgsqlSettings destination)
    {
        destination.ConnectionString = source.ConnectionString;
        destination.DisableHealthChecks = source.DisableHealthChecks;
        destination.DisableMetrics = source.DisableMetrics;
        destination.DisableTracing = source.DisableTracing;
    }

    private static void ConfigureDataSourceBuilder(AzureNpgsqlSettings settings, NpgsqlDataSourceBuilder dataSourceBuilder)
    {
        // The connection string requires the username to be provided. Since it will depend on the Managed Identity that is used
        // we attempt to get the username from the access token.

        var credential = settings.Credential ?? new DefaultAzureCredential();

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
            // The user will be hinted to provide a username by using the configureDataSourceBuilder callback.
        }

        if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Password))
        {
            // The token is not cached since it is refreshed for each new physical connection, or when it has expired.

            dataSourceBuilder.UsePasswordProvider(
                passwordProvider: _ => credential.GetToken(ManagedIdentityTokenCredentialHelpers.DatabaseForPostgresSqlTokenRequestContext, default).Token,
                passwordProviderAsync: async (_, ct) => (await credential.GetTokenAsync(ManagedIdentityTokenCredentialHelpers.DatabaseForPostgresSqlTokenRequestContext, default).ConfigureAwait(false)).Token
            );
        }
    }
}
