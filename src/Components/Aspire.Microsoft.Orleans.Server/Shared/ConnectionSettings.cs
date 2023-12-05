// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Orleans.Shared;

internal sealed class ConnectionSettings
{
    /// <summary>
    /// Gets or sets the name of the connection string containing connection details.
    /// </summary>
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the provider type name.
    /// </summary>
    public string? ProviderType { get; set; }
}
