// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire;
using Aspire.Azure.Npgsql;
using Azure.Core;
using Azure.Identity;
using HealthChecks.NpgSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for connecting to an Azure Database for PostgreSQL with Npgsql client
/// </summary>
public static class AspireAzureNpgsqlExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Azure:Npgsql";
    private const string AzureDatabaseForPostgresSqlScope = "https://ossrdbms-aad.database.windows.net/.default";

    private static readonly TokenRequestContext s_databaseForPostgresSqlTokenRequestContext = new([AzureDatabaseForPostgresSqlScope]);

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
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureNpgsqlSettings.ConnectionString"/> is not provided or the <see cref="AzureNpgsqlSettings.Credential"/> is invalid.</exception>
    public static void AddAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string connectionName, Action<AzureNpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
        => AddAzureNpgsqlDataSource(builder, configureSettings, connectionName, serviceKey: null, configureDataSourceBuilder: configureDataSourceBuilder);

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
    /// <exception cref="InvalidOperationException">Thrown when mandatory <see cref="AzureNpgsqlSettings.ConnectionString"/> is not provided or the <see cref="AzureNpgsqlSettings.Credential"/> is invalid.</exception>
    public static void AddKeyedAzureNpgsqlDataSource(this IHostApplicationBuilder builder, string name, Action<AzureNpgsqlSettings>? configureSettings = null, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        AddAzureNpgsqlDataSource(builder, configureSettings, connectionName: name, serviceKey: name, configureDataSourceBuilder: configureDataSourceBuilder);
    }

    private static void AddAzureNpgsqlDataSource(IHostApplicationBuilder builder,
        Action<AzureNpgsqlSettings>? configureSettings, string connectionName, object? serviceKey, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionName);

        AzureNpgsqlSettings settings = new();
        var configSection = builder.Configuration.GetSection(DefaultConfigSectionName);
        var namedConfigSection = configSection.GetSection(connectionName);
        configSection.Bind(settings);
        namedConfigSection.Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configureSettings?.Invoke(settings);

        builder.RegisterNpgsqlServices(settings, connectionName, serviceKey, configureDataSourceBuilder);

        // Same as SqlClient connection pooling is on by default and can be handled with connection string
        // https://www.npgsql.org/doc/connection-string-parameters.html#pooling
        if (!settings.DisableHealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "AzurePostgreSql" : $"AzurePostgreSql_{connectionName}",
                sp => new NpgSqlHealthCheck(
                    new NpgSqlHealthCheckOptions(serviceKey is null
                        ? sp.GetRequiredService<NpgsqlDataSource>()
                        : sp.GetRequiredKeyedService<NpgsqlDataSource>(serviceKey))),
                failureStatus: default,
                tags: default,
                timeout: default));
        }

        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracerProviderBuilder =>
                {
                    tracerProviderBuilder.AddNpgsql();
                });
        }

        if (!settings.DisableMetrics)
        {
            builder.Services.AddOpenTelemetry()
                .WithMetrics(NpgsqlCommon.AddNpgsqlMetrics);
        }
    }

    private static void RegisterNpgsqlServices(this IHostApplicationBuilder builder, AzureNpgsqlSettings settings, string connectionName, object? serviceKey, Action<NpgsqlDataSourceBuilder>? configureDataSourceBuilder)
    {
        builder.Services.AddNpgsqlDataSource(settings.ConnectionString ?? string.Empty, dataSourceBuilder =>
        {
            // The connection string required the username to be provided. Since it will depend on the Managed Identity that is used
            // we attempt to get the username from the access token.

            if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Username) ||
                string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Password))
            {
                var credential = settings.Credential ?? new DefaultAzureCredential();
                var token = credential.GetToken(s_databaseForPostgresSqlTokenRequestContext, default);

                if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Username))
                {
                    if (TryGetUsernameFromToken(token.Token, out var username))
                    {
                        dataSourceBuilder.ConnectionStringBuilder.Username = username;
                    }
                    else
                    {
                        throw new InvalidOperationException("Could not determine username from token claims");
                    }
                }

                if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Password))
                {
                    dataSourceBuilder.UsePasswordProvider(
                        passwordProvider: _ => token.Token,
                        passwordProviderAsync: (_, ct) => ValueTask.FromResult(token.Token)
                    );
                }
            }

            // Delay validating the ConnectionString until the DataSource is requested. This ensures an exception doesn't happen until a Logger is established.
            ConnectionStringValidation.ValidateConnectionString(settings.ConnectionString, connectionName, DefaultConfigSectionName);

            configureDataSourceBuilder?.Invoke(dataSourceBuilder);
        }, serviceKey: serviceKey);
    }

    private static bool TryGetUsernameFromToken(string jwtToken, out string? username)
    {
        username = null;

        // Split the token into its parts (Header, Payload, Signature)
        var tokenParts = jwtToken.Split('.');
        if (tokenParts.Length != 3)
        {
            return false;
        }

        // The payload is the second part, Base64Url encoded
        var payload = tokenParts[1];

        // Decode the payload from Base64Url
        var decodedBytes = Convert.FromBase64String(payload);

        // Parse the decoded payload as JSON
        var reader = new Utf8JsonReader(decodedBytes);
        var payloadJson = JsonElement.ParseValue(ref reader);

        // Try to get the username from 'upn', 'preferred_username', or 'unique_name' claims
        if (payloadJson.TryGetProperty("upn", out var upn))
        {
            username = upn.GetString();
        }
        else if (payloadJson.TryGetProperty("preferred_username", out var preferredUsername))
        {
            username = preferredUsername.GetString();
        }
        else if (payloadJson.TryGetProperty("unique_name", out var uniqueName))
        {
            username = uniqueName.GetString();
        }

        return username != null;
    }
}
