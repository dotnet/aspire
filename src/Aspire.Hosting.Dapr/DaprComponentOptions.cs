// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Options for configuring a Dapr component.
/// </summary>
[Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
public sealed record DaprComponentOptions
{
    /// <summary>
    /// Gets or sets the path to the component configuration file.
    /// </summary>
    /// <remarks>
    /// If specified, the folder containing the configuration file will be added to all associated Dapr sidecars' resources paths.
    /// </remarks>
    public string? LocalPath { get; init; }
}
