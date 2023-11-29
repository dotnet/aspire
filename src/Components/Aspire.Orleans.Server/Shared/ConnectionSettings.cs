// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Orleans.Shared;

internal sealed class ConnectionSettings
{
    // Name of the connection string to retrieve
    public string? ConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the well-known provider type, see <see cref="OrleansServerSettingConstants"/>.
    /// </summary>
    public string? ConnectionType { get; set; }
}
