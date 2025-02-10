// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Display properties of the endpoint to be displayed in UI.
/// </summary>
public sealed class EndpointDisplayProperties
{
    /// <summary>
    /// Display name of the endpoint, to be displayed in the Aspire Dashboard. An empty display name will default to the endpoint name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Integer to control visual ordering of endpoints in the Aspire Dashboard. Higher values are displayed first.
    /// Ties are broken by protocol type first (https before others), then by endpoint name.
    /// </summary>
    public int SortOrder { get; set; }
}
