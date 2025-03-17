// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Azure.Data.AppConfiguration;

/// <summary>
/// Provides the client configuration settings for connecting to Azure App Configuration.
/// </summary>
public sealed class AzureDataAppConfigurationSettings
{
    /// <summary>
    /// A <see cref="Uri"/> to the App Configuration store on which the client operates.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure App Configuration.
    /// </summary>
    public TokenCredential? Credential { get; set; }
}
