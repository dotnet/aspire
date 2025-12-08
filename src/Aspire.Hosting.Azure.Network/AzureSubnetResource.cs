// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Network;
using Azure.Provisioning;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Subnet resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="subnetName">The subnet name.</param>
/// <param name="addressPrefix">The address prefix for the subnet.</param>
/// <param name="parent">The parent Virtual Network resource.</param>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureSubnetResource(string name, string subnetName, string addressPrefix, AzureVirtualNetworkResource parent)
    : Resource(name), IResourceWithParent<AzureVirtualNetworkResource>
{
    private string _subnetName = ThrowIfNullOrEmpty(subnetName);
    private string _addressPrefix = ThrowIfNullOrEmpty(addressPrefix);

    /// <summary>
    /// The subnet name.
    /// </summary>
    public string SubnetName
    {
        get => _subnetName;
        set => _subnetName = ThrowIfNullOrEmpty(value);
    }

    /// <summary>
    /// The address prefix for the subnet (e.g., "10.0.1.0/24").
    /// </summary>
    public string AddressPrefix
    {
        get => _addressPrefix;
        set => _addressPrefix = value;
    }

    /// <summary>
    /// Gets the subnet Id output reference.
    /// </summary>
    public BicepOutputReference Id => new($"{subnetName}_Id", parent);

    /// <summary>
    /// Gets the parent Azure Virtual Network resource.
    /// </summary>
    public AzureVirtualNetworkResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the NAT Gateway resource associated with this subnet, if any.
    /// </summary>
    public AzureNatGatewayResource? NatGateway { get; internal set; }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => !string.IsNullOrEmpty(argument) ? argument : throw new ArgumentNullException(paramName);

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    internal SubnetResource ToProvisioningEntity(AzureResourceInfrastructure infra)
    {
        var subnet = new SubnetResource(Infrastructure.NormalizeBicepIdentifier(Name))
        {
            Name = SubnetName,
            AddressPrefix = AddressPrefix,
            // TODO: DefaultOutboundAccess = DefaultOutboundAccess
        };

        if (NatGateway is not null)
        {
            subnet.NatGatewayId = NatGateway.Id.AsProvisioningParameter(infra);
        }

        if (this.TryGetLastAnnotation<AzureSubnetServiceDelegationAnnotation>(out var serviceDelegationAnnotation))
        {
            subnet.Delegations.Add(new ServiceDelegation()
            {
                Name = serviceDelegationAnnotation.Name,
                ServiceName = serviceDelegationAnnotation.ServiceName
            });
        }

        // add a provisioning output for the subnet ID so it can be referenced by other resources
        infra.Add(new ProvisioningOutput(Id.Name, typeof(string))
        {
            Value = subnet.Id
        });

        return subnet;
    }
}
