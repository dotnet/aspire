// Assembly 'Aspire.Azure.Data.Tables'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.Data.Tables;

/// <summary>
/// Provides the client configuration settings for connecting to Azure Tables.
/// </summary>
public sealed class AzureDataTablesSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the table service account. 
    /// </summary>
    /// <remarks>
    /// If <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ConnectionString" /> is set, it overrides <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.ServiceUri" /> and <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.Credential" />.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="T:System.Uri" /> referencing the table service account.
    /// This is likely to be similar to "https://{account_name}.table.core.windows.net/" or "https://{account_name}.table.cosmos.azure.com/".
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="P:Aspire.Azure.Data.Tables.AzureDataTablesSettings.Credential" /> to establish the connection.
    /// </remarks>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Tables.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the health check is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool HealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public AzureDataTablesSettings();
}
