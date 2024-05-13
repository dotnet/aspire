// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Microsoft.Data.SqlClient;
using HealthChecks.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for configuring SqlClient connection to Azure SQL, MS SQL server
/// </summary>
public static class AspireSqlServerSqlClientExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Microsoft:Data:SqlClient";

    private static string EscapeIdentifier(string identifier) => identifier.Replace("]", "]]");

    /// <summary>
    /// Opens a database connection with the property settings specified by the <see cref="SqlConnection.ConnectionString"/>. If the database does not exist it attempts to create it.
    /// </summary>
    /// <param name="connection">The <see cref="Microsoft.Data.SqlClient.SqlConnection" />.</param>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="Microsoft.Data.SqlClient.SqlConnection.Open()"/> but adds exception handling to detect
    /// a <see cref="SqlException"/> with a <see cref="SqlException.Number"/> of 4060 (Database not found). If this exception is
    /// throw the connection is closed and the connection string updated to connect to the <c>master</c> database.
    /// </para>
    /// <para>
    /// If database creation succeeds then the connection is changed to use the original database, if creation fails then
    /// the original exception is thrown.
    /// </para>
    /// </remarks>
    public static void OpenWithCreate(this SqlConnection connection)
    {
        try
        {
            connection.Open();
        }
        catch (SqlException ex) when (ex.Number == 4060)
        {
            connection.Close();

            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            var originalDatabase = builder.InitialCatalog;

            builder.InitialCatalog = "master";
            connection.ConnectionString = builder.ConnectionString;

            connection.Open();

            var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE [{EscapeIdentifier(originalDatabase)}]";
            createCommand.ExecuteNonQuery();

            connection.ChangeDatabase(originalDatabase);
        }
    }

    /// <summary>
    /// Opens a database connection with the property settings specified by the <see cref="SqlConnection.ConnectionString"/>. If the database does not exist it attempts to create it.
    /// </summary>
    /// <param name="connection">The <see cref="Microsoft.Data.SqlClient.SqlConnection" />.</param>
    /// <param name="overrides">Options to override default connection open behavior.</param>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="Microsoft.Data.SqlClient.SqlConnection.Open()"/> but adds exception handling to detect
    /// a <see cref="SqlException"/> with a <see cref="SqlException.Number"/> of 4060 (Database not found). If this exception is
    /// throw the connection is closed and the connection string updated to connect to the <c>master</c> database.
    /// </para>
    /// <para>
    /// If database creation succeeds then the connection is changed to use the original database, if creation fails then
    /// the original exception is thrown.
    /// </para>
    /// </remarks>
    public static void OpenWithCreate(this SqlConnection connection, SqlConnectionOverrides overrides)
    {
        try
        {
            connection.Open(overrides);
        }
        catch (SqlException ex) when (ex.Number == 4060)
        {
            connection.Close();

            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            var originalDatabase = builder.InitialCatalog;

            builder.InitialCatalog = "master";
            connection.ConnectionString = builder.ConnectionString;

            connection.Open(overrides);

            var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE [{EscapeIdentifier(originalDatabase)}]";
            createCommand.ExecuteNonQuery();

            connection.ChangeDatabase(originalDatabase);
        }
    }

    /// <summary>
    /// Opens a database connection with the property settings specified by the <see cref="SqlConnection.ConnectionString"/>. If the database does not exist it attempts to create it.
    /// </summary>
    /// <param name="connection">The <see cref="Microsoft.Data.SqlClient.SqlConnection" />.</param>
    /// <param name="cancellationToken"></param>
    /// <remarks>
    /// <para>
    /// This method wraps <see cref="Microsoft.Data.SqlClient.SqlConnection.Open()"/> but adds exception handling to detect
    /// a <see cref="SqlException"/> with a <see cref="SqlException.Number"/> of 4060 (Database not found). If this exception is
    /// throw the connection is closed and the connection string updated to connect to the <c>master</c> database.
    /// </para>
    /// <para>
    /// If database creation succeeds then the connection is changed to use the original database, if creation fails then
    /// the original exception is thrown.
    /// </para>
    /// </remarks>
    public static async Task OpenWithCreateAsync(this SqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == 4060)
        {
            connection.Close();

            var builder = new SqlConnectionStringBuilder(connection.ConnectionString);
            var originalDatabase = builder.InitialCatalog;

            builder.InitialCatalog = "master";
            connection.ConnectionString = builder.ConnectionString;

            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE [{EscapeIdentifier(originalDatabase)}]";
            await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await connection.ChangeDatabaseAsync(originalDatabase, cancellationToken)
                            .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Registers 'Scoped' <see cref="SqlConnection" /> factory for connecting Azure SQL, MS SQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">A name used to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional delegate that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/>  is not provided in configuration section.</exception>
    public static void AddSqlServerClient(this IHostApplicationBuilder builder, string connectionName, Action<MicrosoftDataSqlClientSettings>? configureSettings = null)
        => AddSqlClient(builder, DefaultConfigSectionName, configureSettings, connectionName, serviceKey: null);

    /// <summary>
    /// Registers 'Scoped' <see cref="SqlConnection" /> factory for given <paramref name="name"/> for connecting Azure SQL, MsSQL database using Microsoft.Data.SqlClient.
    /// Configures health check, logging and telemetry for the SqlClient.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="name">The name of the component, which is used as the <see cref="ServiceDescriptor.ServiceKey"/> of the service and also to retrieve the connection string from the ConnectionStrings configuration section.</param>
    /// <param name="configureSettings">An optional method that can be used for customizing options. It's invoked after the settings are read from the configuration.</param>
    /// <remarks>Reads the configuration from "Aspire:Microsoft:Data:SqlClient:{name}" section.</remarks>
    /// <exception cref="InvalidOperationException">If required <see cref="MicrosoftDataSqlClientSettings.ConnectionString"/> is not provided in configuration section.</exception>
    public static void AddKeyedSqlServerClient(this IHostApplicationBuilder builder, string name, Action<MicrosoftDataSqlClientSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        AddSqlClient(builder, $"{DefaultConfigSectionName}:{name}", configureSettings, connectionName: name, serviceKey: name);
    }

    private static void AddSqlClient(IHostApplicationBuilder builder, string configurationSectionName,
        Action<MicrosoftDataSqlClientSettings>? configure, string connectionName, object? serviceKey)
    {
        ArgumentNullException.ThrowIfNull(builder);

        MicrosoftDataSqlClientSettings settings = new();
        builder.Configuration.GetSection(configurationSectionName).Bind(settings);

        if (builder.Configuration.GetConnectionString(connectionName) is string connectionString)
        {
            settings.ConnectionString = connectionString;
        }

        configure?.Invoke(settings);

        // delay validating the ConnectionString until the SqlConnection is requested. This ensures an exception doesn't happen until a Logger is established.
        string GetConnectionString()
        {
            var connectionString = settings.ConnectionString;
            ConnectionStringValidation.ValidateConnectionString(connectionString, connectionName, DefaultConfigSectionName);
            return connectionString!;
        }

        if (serviceKey is null)
        {
            builder.Services.AddScoped(_ => new SqlConnection(GetConnectionString()));
        }
        else
        {
            builder.Services.AddKeyedScoped(serviceKey, (_, __) => new SqlConnection(GetConnectionString()));
        }

        // SqlClient Data Provider (Microsoft.Data.SqlClient) handles connection pooling automatically and it's on by default
        // https://learn.microsoft.com/sql/connect/ado-net/sql-server-connection-pooling
        if (!settings.DisableTracing)
        {
            builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddSqlClientInstrumentation();
            });
        }

        if (!settings.DisableHealthChecks)
        {
            builder.TryAddHealthCheck(new HealthCheckRegistration(
                serviceKey is null ? "SqlServer" : $"SqlServer_{connectionName}",
                sp => new SqlServerHealthCheck(new SqlServerHealthCheckOptions()
                {
                    ConnectionString = settings.ConnectionString ?? string.Empty
                }),
                failureStatus: default,
                tags: default,
                timeout: default));
        }
    }
}
