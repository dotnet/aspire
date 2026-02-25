// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Annotation that stores auto-detected deployment script configuration for SQL servers with private endpoints.
/// </summary>
internal sealed record AutoDeploymentScriptConfigAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the virtual network for auto-creating the ACI subnet. Null if user provided an explicit subnet.
    /// </summary>
    public AzureVirtualNetworkResource? VNet { get; init; }

    /// <summary>
    /// Gets the allocated CIDR for the ACI subnet. Null if user provided an explicit subnet.
    /// </summary>
    public string? AciSubnetCidr { get; init; }

    /// <summary>
    /// Gets the subnet where private endpoints are created. Always set when a PE scenario is detected.
    /// </summary>
    public required AzureSubnetResource PeSubnet { get; init; }

    /// <summary>
    /// Gets whether a new storage account should be auto-created. False when user provided explicit storage.
    /// </summary>
    public bool AutoCreateStorage { get; init; }

    /// <summary>
    /// Gets whether this annotation includes auto-subnet configuration.
    /// </summary>
    public bool HasAutoSubnet => VNet is not null && AciSubnetCidr is not null;
}
