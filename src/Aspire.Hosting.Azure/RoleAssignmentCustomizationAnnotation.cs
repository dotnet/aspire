// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an annotation that provides a callback to be executed for customizing
/// role assignments to Azure resources.
/// </summary>
public class RoleAssignmentCustomizationAnnotation(Action<AzureResourceInfrastructure, AzureProvisioningResource> configure) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback action to customize the role assignments for the Azure resource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="AzureResourceInfrastructure"/> is the bicep module that assigns roles
    /// to the compute resource.
    /// </para>
    /// <para>
    /// The <see cref="AzureProvisioningResource"/> is the Azure resource the compute resource will interact with.
    /// </para>
    /// </remarks>
    public Action<AzureResourceInfrastructure, AzureProvisioningResource> Configure { get; } = configure;

    /// <summary>
    /// The Azure resource that the current resource will interact with.
    /// </summary>
    /// <remarks>
    /// This is set when the <see cref="RoleAssignmentCustomizationAnnotation"/> is applied to a compute resource (e.g., Project or Container).
    /// </remarks>
    public AzureProvisioningResource? Target { get; set; }
}
