// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Npgsql;
using Azure.Core;

namespace Aspire.Azure.Npgsql;

/// <summary>
/// Provides the client configuration settings for connecting to an Azure Database for PostgreSQL using Npgsql.
/// </summary>
public sealed class AzureNpgsqlSettings : NpgsqlSettings
{
    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Database for PostgreSQL.
    /// </summary>
    public TokenCredential? Credential { get; set; }
}
