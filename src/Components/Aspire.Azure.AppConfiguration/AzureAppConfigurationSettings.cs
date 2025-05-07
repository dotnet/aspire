// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AppConfiguration;

/// <summary>
/// Provides the client configuration settings for connecting to Azure App Configuration.
/// </summary>
public sealed class AzureAppConfigurationSettings : IConnectionStringSettings
{
    /// <summary>
    /// A <see cref="Uri"/> to the App Configuration store on which the client operates. Appears as "Endpoint" in the Azure portal.
    /// This is likely to be similar to "https://{store_name}.azconfig.io".
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure App Configuration.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString) &&
            Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            Endpoint = uri;
        }
    }
}
