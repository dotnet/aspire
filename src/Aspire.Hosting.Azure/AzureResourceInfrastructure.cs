// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Expressions;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Azure Provisioning <see cref="Infrastructure" /> which represents the root Bicep module that is generated for an Azure resource.
/// </summary>
public class AzureResourceInfrastructure : Infrastructure
{
    internal AzureResourceInfrastructure(AzureProvisioningResource resource, string name) : base(name)
    {
        Resource = resource;

        // Always add a default location parameter.
        // azd assumes there will be a location parameter for every module.
        // The Infrastructure location resolver will resolve unset Location properties to this parameter.
        Add(new ProvisioningParameter("location", typeof(string))
        {
            Description = "The location for the resource(s) to be deployed.",
            Value = BicepFunction.GetResourceGroup().Location
        });
    }

    /// <summary>
    /// The Azure construct resource that this resource module construct represents.
    /// </summary>
    public AzureProvisioningResource Resource { get; }

    /// <summary>
    /// The common principalId parameter injected into most Aspire-based Bicep files.
    /// </summary>
    public ProvisioningParameter PrincipalIdParameter => new ProvisioningParameter("principalId", typeof(string));

    /// <summary>
    /// The common principalType parameter injected into most Aspire-based Bicep files.
    /// </summary>
    public ProvisioningParameter PrincipalTypeParameter => new ProvisioningParameter("principalType", typeof(string));

    /// <summary>
    /// The common principalName parameter injected into some Aspire-based Bicep files.
    /// </summary>
    public ProvisioningParameter PrincipalNameParameter => new ProvisioningParameter("principalName", typeof(string));

    internal IEnumerable<ProvisioningParameter> GetParameters() => GetResources().OfType<ProvisioningParameter>();
}
