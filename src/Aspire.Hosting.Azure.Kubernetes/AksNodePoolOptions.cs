// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kubernetes;

/// <summary>
/// Provides options for configuring an AKS system or user node pool.
/// </summary>
/// <remarks>
/// Each configuration callback receives fresh defaults. Values are copied into the node pool
/// configuration after the callback completes.
/// </remarks>
[AspireDto]
public sealed class AksNodePoolOptions
{
    /// <summary>
    /// Gets or sets the VM size for nodes. Defaults to <c>Standard_D2s_v5</c>.
    /// </summary>
    public string VmSize { get; set; } = "Standard_D2s_v5";

    /// <summary>
    /// Gets or sets the minimum node count for autoscaling. Defaults to 1.
    /// System pools require at least 1; user pools can scale to 0.
    /// </summary>
    public int MinCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum node count for autoscaling. Defaults to 3.
    /// Must be at least the minimum node count.
    /// </summary>
    public int MaxCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the Linux OS SKU, such as <c>AzureLinux</c> or <c>Ubuntu</c>.
    /// Defaults to <c>null</c>, which leaves the SKU unspecified so AKS chooses its default.
    /// </summary>
    /// <remarks>
    /// This selects the OS SKU, not a specific OS image version. Azure validates SKU availability
    /// and compatibility with the node pool.
    /// </remarks>
    public string? OsSku { get; set; }
}
