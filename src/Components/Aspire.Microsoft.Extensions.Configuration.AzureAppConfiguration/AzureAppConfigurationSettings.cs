// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Microsoft.Extensions.Configuration.AzureAppConfiguration;

/// <summary>
/// Provides the client configuration settings for connecting to Azure App Configuration.
/// </summary>
public sealed class AzureAppConfigurationSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the App Configuration service. 
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="Uri"/> to the App Configuration store on which the client operates. Appears as "Endpoint" in the Azure portal.
    /// This is likely to be similar to "https://{store_name}.azconfig.io".
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure App Configuration.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Determines the behavior of the App Configuration provider when an exception occurs while loading data from server.
    /// If false, the exception is thrown. If true, the exception is suppressed and no configuration values are populated from Azure App Configuration.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the App Configuration provider health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Indicates whether to connect to the Azure App Configuration service without authentication.
    /// </summary>
    /// <remarks>
    /// This is only supported when using the Azure App Configuration emulator with anonymous authentication enabled.
    /// It will not be used for the real Azure App Configuration service.
    /// </remarks>
    internal bool AnonymousAccess { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                Endpoint = uri;
            }
            else
            {
                var connectionBuilder = new StableConnectionStringBuilder(connectionString);

                if (connectionBuilder.TryGetValue("Anonymous", out string enabled))
                {
                    AnonymousAccess = Boolean.Parse(enabled);

                    connectionBuilder.Remove("Anonymous");
                }

                ConnectionString = connectionBuilder.ConnectionString;
            }
        }
    }
}
