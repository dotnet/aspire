// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Network Security Group resource.
/// </summary>
/// <remarks>
/// Use <see cref="AzureVirtualNetworkExtensions.AddNetworkSecurityGroup"/> to create an instance
/// and <see cref="AzureVirtualNetworkExtensions.WithSecurityRule"/> to add security rules.
/// Associate the NSG with a subnet using <see cref="AzureVirtualNetworkExtensions.WithNetworkSecurityGroup"/>.
/// </remarks>
public class AzureNetworkSecurityGroupResource : Resource, IResourceWithParent<AzureVirtualNetworkResource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureNetworkSecurityGroupResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="parent">The parent Virtual Network resource.</param>
    public AzureNetworkSecurityGroupResource(string name, AzureVirtualNetworkResource parent)
        : base(name)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>
    /// Gets the parent Azure Virtual Network resource.
    /// </summary>
    public AzureVirtualNetworkResource Parent { get; }

    internal List<AzureSecurityRule> SecurityRules { get; } = [];

    /// <summary>
    /// Converts the current instance to provisioning entities: the NSG and its security rules.
    /// </summary>
    internal (NetworkSecurityGroup Nsg, List<SecurityRule> Rules) ToProvisioningEntity()
    {
        var nsg = new NetworkSecurityGroup(Infrastructure.NormalizeBicepIdentifier(Name));

        var rules = new List<SecurityRule>();
        foreach (var rule in SecurityRules)
        {
            var ruleIdentifier = Infrastructure.NormalizeBicepIdentifier($"{nsg.BicepIdentifier}_{rule.Name}");
            var securityRule = new SecurityRule(ruleIdentifier)
            {
                Name = rule.Name,
                Priority = rule.Priority,
                Direction = rule.Direction,
                Access = rule.Access,
                Protocol = rule.Protocol,
                SourceAddressPrefix = rule.SourceAddressPrefix,
                SourcePortRange = rule.SourcePortRange,
                DestinationAddressPrefix = rule.DestinationAddressPrefix,
                DestinationPortRange = rule.DestinationPortRange,
                Parent = nsg,
            };
            rules.Add(securityRule);
        }

        return (nsg, rules);
    }
}
