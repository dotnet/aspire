// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Aspire.Hosting.ApplicationModel;

using Aspire.Hosting.Dapr.Models.ComponentSpec;

namespace Aspire.Hosting.Dapr;

/// <summary>
/// Options for configuring a Dapr component.
/// </summary>
public sealed record DaprComponentOptions
{
    /// <summary>
    /// Gets or sets the path to the component configuration file.
    /// </summary>
    /// <remarks>
    /// If specified, the folder containing the configuration file will be added to all associated Dapr sidecars' resources paths.
    /// </remarks>
    public string? LocalPath { get; init; }

    /// <summary>
    /// Gets or sets the component configuration
    /// </summary>
    public List<MetadataValue>? Configuration { get; init; }

    /// <summary>
    /// The optional secret store ref.
    /// Is required if the <see cref="Metadata"/> contains a reference to a secret.
    /// </summary>
    public IDaprComponentResource? SecretStore { get; init; }
}
