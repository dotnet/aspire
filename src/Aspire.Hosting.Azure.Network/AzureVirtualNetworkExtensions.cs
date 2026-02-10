// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Network;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Virtual Network resources to the application model.
/// </summary>
public static class AzureVirtualNetworkExtensions
{
    /// <summary>
    /// Adds an Azure Virtual Network resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure Virtual Network resource.</param>
    /// <param name="addressPrefix">The address prefix for the virtual network (e.g., "10.0.0.0/16"). If null, defaults to "10.0.0.0/16".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureVirtualNetworkResource}"/>.</returns>
    /// <example>
    /// This example creates a virtual network with a subnet for private endpoints:
    /// <code>
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("pe-subnet", "10.0.1.0/24");
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureVirtualNetworkResource> AddAzureVirtualNetwork(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string? addressPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        AzureVirtualNetworkResource resource = new(name, ConfigureVirtualNetwork, addressPrefix);

        return AddAzureVirtualNetworkCore(builder, resource);
    }

    /// <summary>
    /// Adds an Azure Virtual Network resource to the application model with a parameterized address prefix.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure Virtual Network resource.</param>
    /// <param name="addressPrefix">The parameter resource containing the address prefix for the virtual network (e.g., "10.0.0.0/16").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureVirtualNetworkResource}"/>.</returns>
    /// <example>
    /// This example creates a virtual network with a parameterized address prefix:
    /// <code>
    /// var vnetPrefix = builder.AddParameter("vnetPrefix");
    /// var vnet = builder.AddAzureVirtualNetwork("vnet", vnetPrefix);
    /// var subnet = vnet.AddSubnet("pe-subnet", "10.0.1.0/24");
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureVirtualNetworkResource> AddAzureVirtualNetwork(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource> addressPrefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(addressPrefix);

        builder.AddAzureProvisioning();

        AzureVirtualNetworkResource resource = new(name, ConfigureVirtualNetwork, addressPrefix.Resource);

        return AddAzureVirtualNetworkCore(builder, resource);
    }

    private static IResourceBuilder<AzureVirtualNetworkResource> AddAzureVirtualNetworkCore(
        IDistributedApplicationBuilder builder,
        AzureVirtualNetworkResource resource)
    {
        if (builder.ExecutionContext.IsRunMode)
        {
            // In run mode, we don't want to add the resource to the builder.
            return builder.CreateResourceBuilder(resource);
        }

        return builder.AddResource(resource);
    }

    private static void ConfigureVirtualNetwork(AzureResourceInfrastructure infra)
    {
        var azureResource = (AzureVirtualNetworkResource)infra.AspireResource;

        var vnet = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
            (identifier, name) =>
            {
                var resource = VirtualNetwork.FromExisting(identifier);
                resource.Name = name;
                return resource;
            },
            (infrastructure) =>
            {
                var vnet = new VirtualNetwork(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                };

                // Set the address prefix from either the literal string or the parameter
                if (azureResource.AddressPrefix is { } addressPrefix)
                {
                    vnet.AddressSpace = new VirtualNetworkAddressSpace()
                    {
                        AddressPrefixes = { addressPrefix }
                    };
                }
                else if (azureResource.AddressPrefixParameter is { } addressPrefixParameter)
                {
                    vnet.AddressSpace = new VirtualNetworkAddressSpace()
                    {
                        AddressPrefixes = { addressPrefixParameter.AsProvisioningParameter(infrastructure) }
                    };
                }

                return vnet;
            });

        // Add subnets
        if (azureResource.Subnets.Count > 0)
        {
            // Chain subnet provisioning to ensure deployment doesn't fail
            // due to parallel creation of subnets within the VNet.
            ProvisionableResource? dependsOn = null;
            foreach (var subnet in azureResource.Subnets)
            {
                var cdkSubnet = subnet.ToProvisioningEntity(infra, dependsOn);
                cdkSubnet.Parent = vnet;
                infra.Add(cdkSubnet);

                dependsOn = cdkSubnet;
            }
        }

        // Output the VNet ID for references
        infra.Add(new ProvisioningOutput("id", typeof(string))
        {
            Value = vnet.Id
        });

        // We need to output name so it can be referenced by others.
        infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = vnet.Name });
    }

    /// <summary>
    /// Adds an Azure Subnet to the Virtual Network.
    /// </summary>
    /// <param name="builder">The Virtual Network resource builder.</param>
    /// <param name="name">The name of the subnet resource.</param>
    /// <param name="addressPrefix">The address prefix for the subnet (e.g., "10.0.1.0/24").</param>
    /// <param name="subnetName">The subnet name in Azure. If null, the resource name is used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    /// <example>
    /// This example adds a subnet to a virtual network:
    /// <code>
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("my-subnet", "10.0.1.0/24");
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureSubnetResource> AddSubnet(
        this IResourceBuilder<AzureVirtualNetworkResource> builder,
        [ResourceName] string name,
        string addressPrefix,
        string? subnetName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(addressPrefix);

        subnetName ??= name;

        var subnet = new AzureSubnetResource(name, subnetName, addressPrefix, builder.Resource);

        return AddSubnetCore(builder, subnet);
    }

    /// <summary>
    /// Adds an Azure Subnet to the Virtual Network with a parameterized address prefix.
    /// </summary>
    /// <param name="builder">The Virtual Network resource builder.</param>
    /// <param name="name">The name of the subnet resource.</param>
    /// <param name="addressPrefix">The parameter resource containing the address prefix for the subnet (e.g., "10.0.1.0/24").</param>
    /// <param name="subnetName">The subnet name in Azure. If null, the resource name is used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    /// <example>
    /// This example adds a subnet with a parameterized address prefix:
    /// <code>
    /// var subnetPrefix = builder.AddParameter("subnetPrefix");
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("my-subnet", subnetPrefix);
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureSubnetResource> AddSubnet(
        this IResourceBuilder<AzureVirtualNetworkResource> builder,
        [ResourceName] string name,
        IResourceBuilder<ParameterResource> addressPrefix,
        string? subnetName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(addressPrefix);

        subnetName ??= name;

        var subnet = new AzureSubnetResource(name, subnetName, addressPrefix.Resource, builder.Resource);

        return AddSubnetCore(builder, subnet);
    }

    private static IResourceBuilder<AzureSubnetResource> AddSubnetCore(
        IResourceBuilder<AzureVirtualNetworkResource> builder,
        AzureSubnetResource subnet)
    {
        builder.Resource.Subnets.Add(subnet);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // In run mode, we don't want to add the resource to the builder.
            return builder.ApplicationBuilder.CreateResourceBuilder(subnet);
        }

        return builder.ApplicationBuilder.AddResource(subnet)
            .ExcludeFromManifest();
    }

    /// <summary>
    /// Configures the resource to use the specified subnet with appropriate service delegation.
    /// </summary>
    /// <typeparam name="T">The type of resource that supports subnet delegation.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="subnet">The subnet to associate with the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method automatically configures the subnet with the appropriate service delegation
    /// for the target resource type (e.g., "Microsoft.App/environments" for Azure Container Apps).
    /// </remarks>
    /// <example>
    /// This example configures an Azure Container App Environment to use a subnet:
    /// <code>
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("aca-subnet", "10.0.0.0/23");
    ///
    /// var env = builder.AddAzureContainerAppEnvironment("env")
    ///     .WithDelegatedSubnet(subnet);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithDelegatedSubnet<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureSubnetResource> subnet)
        where T : IAzureDelegatedSubnetResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(subnet);

        var target = builder.Resource;

        // Store the subnet ID reference on the target resource via annotation
        builder.WithAnnotation(
            new DelegatedSubnetAnnotation(ReferenceExpression.Create($"{subnet.Resource.Id}")));

        // Add service delegation annotation to the subnet
        subnet.WithAnnotation(new AzureSubnetServiceDelegationAnnotation(
            target.DelegatedSubnetServiceName,
            target.DelegatedSubnetServiceName));

        return builder;
    }

    /// <summary>
    /// Associates a NAT Gateway with the subnet.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="natGateway">The NAT Gateway to associate with the subnet.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <remarks>
    /// A NAT Gateway provides outbound internet connectivity for resources in the subnet.
    /// A subnet can have at most one NAT Gateway.
    /// </remarks>
    /// <example>
    /// This example creates a subnet with an associated NAT Gateway:
    /// <code>
    /// var natGateway = builder.AddNatGateway("nat");
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("aca-subnet", "10.0.0.0/23")
    ///     .WithNatGateway(natGateway);
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureSubnetResource> WithNatGateway(
        this IResourceBuilder<AzureSubnetResource> builder,
        IResourceBuilder<AzureNatGatewayResource> natGateway)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(natGateway);

        builder.Resource.NatGateway = natGateway.Resource;
        return builder;
    }

    /// <summary>
    /// Associates a Network Security Group with the subnet.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="nsg">The Network Security Group to associate with the subnet.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <example>
    /// This example creates a subnet with an associated Network Security Group:
    /// <code>
    /// var nsg = builder.AddNetworkSecurityGroup("web-nsg");
    /// var vnet = builder.AddAzureVirtualNetwork("vnet");
    /// var subnet = vnet.AddSubnet("web-subnet", "10.0.1.0/24")
    ///     .WithNetworkSecurityGroup(nsg);
    /// </code>
    /// </example>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the subnet already has security rules added via shorthand methods
    /// (<see cref="AllowInbound"/>, <see cref="DenyInbound"/>, <see cref="AllowOutbound"/>, <see cref="DenyOutbound"/>).
    /// Use either shorthand methods or an explicit NSG, not both.
    /// </exception>
    public static IResourceBuilder<AzureSubnetResource> WithNetworkSecurityGroup(
        this IResourceBuilder<AzureSubnetResource> builder,
        IResourceBuilder<AzureNetworkSecurityGroupResource> nsg)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(nsg);

        if (builder.Resource.NetworkSecurityGroup is { IsImplicitlyCreated: true })
        {
            throw new InvalidOperationException(
                $"The subnet '{builder.Resource.Name}' already has an NSG created via shorthand methods. " +
                $"Calling WithNetworkSecurityGroup would replace the existing NSG and discard those rules. " +
                $"Use either shorthand methods (AllowInbound, DenyInbound, etc.) or an explicit NSG, not both.");
        }

        builder.Resource.NetworkSecurityGroup = nsg.Resource;
        return builder;
    }

    /// <summary>
    /// Adds an inbound allow rule to the subnet's Network Security Group.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="port">The destination port range (e.g., "443", "80-443"). Defaults to "*" (any).</param>
    /// <param name="from">The source address prefix (e.g., "AzureLoadBalancer", "Internet", "10.0.0.0/8"). Defaults to "*" (any).</param>
    /// <param name="to">The destination address prefix. Defaults to "*" (any).</param>
    /// <param name="protocol">The network protocol. Defaults to <see cref="SecurityRuleProtocol.Asterisk"/> (any).</param>
    /// <param name="priority">The rule priority (100-4096). If not specified, auto-increments from 100 by 100.</param>
    /// <param name="name">The rule name. If not specified, auto-generated from parameters.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <remarks>
    /// If no Network Security Group has been associated with the subnet, one is automatically created.
    /// </remarks>
    /// <example>
    /// This example allows HTTPS traffic from the Azure Load Balancer:
    /// <code>
    /// var subnet = vnet.AddSubnet("web", "10.0.1.0/24")
    ///     .AllowInbound(port: "443", from: "AzureLoadBalancer", protocol: SecurityRuleProtocol.Tcp)
    ///     .DenyInbound(from: "Internet");
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureSubnetResource> AllowInbound(
        this IResourceBuilder<AzureSubnetResource> builder,
        string? port = null,
        string? from = null,
        string? to = null,
        SecurityRuleProtocol? protocol = null,
        int? priority = null,
        string? name = null)
    {
        return AddSecurityRuleShorthand(builder, SecurityRuleAccess.Allow, SecurityRuleDirection.Inbound, port, from, to, protocol, priority, name);
    }

    /// <summary>
    /// Adds an inbound deny rule to the subnet's Network Security Group.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="port">The destination port range (e.g., "443", "80-443"). Defaults to "*" (any).</param>
    /// <param name="from">The source address prefix (e.g., "Internet", "VirtualNetwork", "10.0.0.0/8"). Defaults to "*" (any).</param>
    /// <param name="to">The destination address prefix. Defaults to "*" (any).</param>
    /// <param name="protocol">The network protocol. Defaults to <see cref="SecurityRuleProtocol.Asterisk"/> (any).</param>
    /// <param name="priority">The rule priority (100-4096). If not specified, auto-increments from 100 by 100.</param>
    /// <param name="name">The rule name. If not specified, auto-generated from parameters.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <remarks>
    /// If no Network Security Group has been associated with the subnet, one is automatically created.
    /// </remarks>
    public static IResourceBuilder<AzureSubnetResource> DenyInbound(
        this IResourceBuilder<AzureSubnetResource> builder,
        string? port = null,
        string? from = null,
        string? to = null,
        SecurityRuleProtocol? protocol = null,
        int? priority = null,
        string? name = null)
    {
        return AddSecurityRuleShorthand(builder, SecurityRuleAccess.Deny, SecurityRuleDirection.Inbound, port, from, to, protocol, priority, name);
    }

    /// <summary>
    /// Adds an outbound allow rule to the subnet's Network Security Group.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="port">The destination port range (e.g., "443", "80-443"). Defaults to "*" (any).</param>
    /// <param name="from">The source address prefix. Defaults to "*" (any).</param>
    /// <param name="to">The destination address prefix (e.g., "Internet", "VirtualNetwork"). Defaults to "*" (any).</param>
    /// <param name="protocol">The network protocol. Defaults to <see cref="SecurityRuleProtocol.Asterisk"/> (any).</param>
    /// <param name="priority">The rule priority (100-4096). If not specified, auto-increments from 100 by 100.</param>
    /// <param name="name">The rule name. If not specified, auto-generated from parameters.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <remarks>
    /// If no Network Security Group has been associated with the subnet, one is automatically created.
    /// </remarks>
    public static IResourceBuilder<AzureSubnetResource> AllowOutbound(
        this IResourceBuilder<AzureSubnetResource> builder,
        string? port = null,
        string? from = null,
        string? to = null,
        SecurityRuleProtocol? protocol = null,
        int? priority = null,
        string? name = null)
    {
        return AddSecurityRuleShorthand(builder, SecurityRuleAccess.Allow, SecurityRuleDirection.Outbound, port, from, to, protocol, priority, name);
    }

    /// <summary>
    /// Adds an outbound deny rule to the subnet's Network Security Group.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="port">The destination port range (e.g., "443", "80-443"). Defaults to "*" (any).</param>
    /// <param name="from">The source address prefix. Defaults to "*" (any).</param>
    /// <param name="to">The destination address prefix (e.g., "Internet", "VirtualNetwork"). Defaults to "*" (any).</param>
    /// <param name="protocol">The network protocol. Defaults to <see cref="SecurityRuleProtocol.Asterisk"/> (any).</param>
    /// <param name="priority">The rule priority (100-4096). If not specified, auto-increments from 100 by 100.</param>
    /// <param name="name">The rule name. If not specified, auto-generated from parameters.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/> for chaining.</returns>
    /// <remarks>
    /// If no Network Security Group has been associated with the subnet, one is automatically created.
    /// </remarks>
    public static IResourceBuilder<AzureSubnetResource> DenyOutbound(
        this IResourceBuilder<AzureSubnetResource> builder,
        string? port = null,
        string? from = null,
        string? to = null,
        SecurityRuleProtocol? protocol = null,
        int? priority = null,
        string? name = null)
    {
        return AddSecurityRuleShorthand(builder, SecurityRuleAccess.Deny, SecurityRuleDirection.Outbound, port, from, to, protocol, priority, name);
    }

    private static IResourceBuilder<AzureSubnetResource> AddSecurityRuleShorthand(
        IResourceBuilder<AzureSubnetResource> builder,
        SecurityRuleAccess access,
        SecurityRuleDirection direction,
        string? port,
        string? from,
        string? to,
        SecurityRuleProtocol? protocol,
        int? priority,
        string? name)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var subnet = builder.Resource;

        // Auto-create NSG if one doesn't exist
        if (subnet.NetworkSecurityGroup is null)
        {
            var nsgName = $"{subnet.Name}-nsg";
            var nsgBuilder = builder.ApplicationBuilder.AddNetworkSecurityGroup(nsgName);
            nsgBuilder.Resource.IsImplicitlyCreated = true;
            subnet.NetworkSecurityGroup = nsgBuilder.Resource;
        }

        var nsgResource = subnet.NetworkSecurityGroup;

        // Auto-increment priority
        var resolvedPriority = priority ?? (nsgResource.SecurityRules.Count == 0
            ? 100
            : nsgResource.SecurityRules.Max(r => r.Priority) + 100);

        // Auto-generate name
        var accessStr = access == SecurityRuleAccess.Allow ? "allow" : "deny";
        var directionStr = direction == SecurityRuleDirection.Inbound ? "inbound" : "outbound";
        var resolvedName = name ?? GenerateRuleName(accessStr, directionStr, port, from);

        var rule = new AzureSecurityRule
        {
            Name = resolvedName,
            Priority = resolvedPriority,
            Direction = direction,
            Access = access,
            Protocol = protocol ?? SecurityRuleProtocol.Asterisk,
            DestinationPortRange = port ?? "*",
        };

        if (from is not null)
        {
            rule.SourceAddressPrefix = from;
        }

        if (to is not null)
        {
            rule.DestinationAddressPrefix = to;
        }

        nsgResource.SecurityRules.Add(rule);

        return builder;
    }

    private static string GenerateRuleName(string access, string direction, string? port, string? from)
    {
        var parts = new List<string> { access, direction };

        if (port is not null)
        {
            parts.Add(port);
        }

        if (from is not null)
        {
            parts.Add(from);
        }

        return string.Join("-", parts);
    }
}
