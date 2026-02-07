// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Utils;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure.Tests;

public class AzureVirtualNetworkExtensionsTests
{
    [Fact]
    public void AddAzureVirtualNetwork_CreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");

        Assert.NotNull(vnet);
        Assert.Equal("myvnet", vnet.Resource.Name);
        Assert.IsType<AzureVirtualNetworkResource>(vnet.Resource);
    }

    [Fact]
    public void AddAzureVirtualNetwork_WithCustomAddressPrefix()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet", "10.1.0.0/16");

        Assert.NotNull(vnet);
        Assert.Equal("myvnet", vnet.Resource.Name);
        Assert.Equal("10.1.0.0/16", vnet.Resource.AddressPrefix);
        Assert.Null(vnet.Resource.AddressPrefixParameter);
    }

    [Fact]
    public void AddAzureVirtualNetwork_WithParameterResource_CreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnetPrefixParam = builder.AddParameter("vnetPrefix");
        var vnet = builder.AddAzureVirtualNetwork("myvnet", vnetPrefixParam);

        Assert.NotNull(vnet);
        Assert.Equal("myvnet", vnet.Resource.Name);
        Assert.Null(vnet.Resource.AddressPrefix);
        Assert.Same(vnetPrefixParam.Resource, vnet.Resource.AddressPrefixParameter);
    }

    [Fact]
    public async Task AddAzureVirtualNetwork_WithParameterResource_GeneratesBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnetPrefixParam = builder.AddParameter("vnetPrefix");
        var vnet = builder.AddAzureVirtualNetwork("myvnet", vnetPrefixParam);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep")
            .UseMethodName("AddAzureVirtualNetwork_WithParameterResource_GeneratesBicep");
    }

    [Fact]
    public void AddSubnet_CreatesSubnetResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", "10.0.1.0/24");

        Assert.NotNull(subnet);
        Assert.Equal("mysubnet", subnet.Resource.Name);
        Assert.Equal("mysubnet", subnet.Resource.SubnetName);
        Assert.Equal("10.0.1.0/24", subnet.Resource.AddressPrefix);
        Assert.Same(vnet.Resource, subnet.Resource.Parent);
    }

    [Fact]
    public void AddSubnet_WithCustomSubnetName()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", "10.0.1.0/24", subnetName: "custom-subnet-name");

        Assert.Equal("mysubnet", subnet.Resource.Name);
        Assert.Equal("custom-subnet-name", subnet.Resource.SubnetName);
        Assert.Equal("10.0.1.0/24", subnet.Resource.AddressPrefix);
    }

    [Fact]
    public void AddSubnet_MultipleSubnets_HaveDifferentParentReferences()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet1 = vnet.AddSubnet("subnet1", "10.0.1.0/24");
        var subnet2 = vnet.AddSubnet("subnet2", "10.0.2.0/24");

        // Both subnets should have the same parent VNet
        Assert.Same(vnet.Resource, subnet1.Resource.Parent);
        Assert.Same(vnet.Resource, subnet2.Resource.Parent);
        // But they should be different resources
        Assert.NotSame(subnet1.Resource, subnet2.Resource);
    }

    [Fact]
    public async Task AddAzureVirtualNetwork_WithSubnets_GeneratesBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        vnet.AddSubnet("subnet1", "10.0.1.0/24")
            .WithAnnotation(new AzureSubnetServiceDelegationAnnotation("ContainerAppsDelegation", "Microsoft.App/environments"));
        vnet.AddSubnet("subnet2", "10.0.2.0/24", subnetName: "custom-subnet-name");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void AddAzureVirtualNetwork_InRunMode_DoesNotAddToBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", "10.0.1.0/24");

        // In run mode, the resource should not be added to the builder's resources
        Assert.DoesNotContain(vnet.Resource, builder.Resources);
        // In run mode, the subnet should not be added to the builder's resources
        Assert.DoesNotContain(subnet.Resource, builder.Resources);
    }

    [Fact]
    public void WithDelegatedSubnet_AddsAnnotationsToSubnetAndTarget()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", "10.0.0.0/23");

        var env = builder.AddAzureContainerAppEnvironment("env")
            .WithDelegatedSubnet(subnet);

        // Verify the target has DelegatedSubnetAnnotation
        var subnetAnnotation = env.Resource.Annotations.OfType<DelegatedSubnetAnnotation>().SingleOrDefault();
        Assert.NotNull(subnetAnnotation);
        Assert.Equal("{myvnet.outputs.mysubnet_Id}", subnetAnnotation.SubnetId.ValueExpression);

        // Verify the subnet has AzureSubnetServiceDelegationAnnotation
        var delegationAnnotation = subnet.Resource.Annotations.OfType<AzureSubnetServiceDelegationAnnotation>().SingleOrDefault();
        Assert.NotNull(delegationAnnotation);
        Assert.Equal("Microsoft.App/environments", delegationAnnotation.ServiceName);
    }

    [Fact]
    public void AddSubnet_WithParameterResource_CreatesSubnetResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var addressPrefixParam = builder.AddParameter("subnetPrefix");
        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", addressPrefixParam);

        Assert.NotNull(subnet);
        Assert.Equal("mysubnet", subnet.Resource.Name);
        Assert.Equal("mysubnet", subnet.Resource.SubnetName);
        Assert.Null(subnet.Resource.AddressPrefix);
        Assert.Same(addressPrefixParam.Resource, subnet.Resource.AddressPrefixParameter);
        Assert.Same(vnet.Resource, subnet.Resource.Parent);
    }

    [Fact]
    public void AddSubnet_WithParameterResource_AndCustomSubnetName()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var addressPrefixParam = builder.AddParameter("subnetPrefix");
        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var subnet = vnet.AddSubnet("mysubnet", addressPrefixParam, subnetName: "custom-subnet-name");

        Assert.Equal("mysubnet", subnet.Resource.Name);
        Assert.Equal("custom-subnet-name", subnet.Resource.SubnetName);
        Assert.Null(subnet.Resource.AddressPrefix);
        Assert.Same(addressPrefixParam.Resource, subnet.Resource.AddressPrefixParameter);
    }

    [Fact]
    public async Task AddSubnet_WithParameterResource_GeneratesBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var addressPrefixParam = builder.AddParameter("subnetPrefix");
        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        vnet.AddSubnet("mysubnet", addressPrefixParam);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddSubnet_WithNatGateway_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var natGw = builder.AddNatGateway("mynat");
        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        vnet.AddSubnet("mysubnet", "10.0.1.0/24")
            .WithNatGateway(natGw);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void AddNetworkSecurityGroup_CreatesResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var nsg = vnet.AddNetworkSecurityGroup("web-nsg");

        Assert.NotNull(nsg);
        Assert.Equal("web-nsg", nsg.Resource.Name);
        Assert.IsType<AzureNetworkSecurityGroupResource>(nsg.Resource);
        Assert.Same(vnet.Resource, nsg.Resource.Parent);
        Assert.Contains(nsg.Resource, vnet.Resource.NetworkSecurityGroups);
    }

    [Fact]
    public void AddNetworkSecurityGroup_InRunMode_DoesNotAddToBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var nsg = vnet.AddNetworkSecurityGroup("web-nsg");

        Assert.DoesNotContain(nsg.Resource, builder.Resources);
    }

    [Fact]
    public async Task AddNetworkSecurityGroup_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        vnet.AddNetworkSecurityGroup("web-nsg");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddNetworkSecurityGroup_WithSecurityRules_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        vnet.AddNetworkSecurityGroup("web-nsg")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            })
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "deny-all-inbound",
                Priority = 4096,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Deny,
                Protocol = SecurityRuleProtocol.Asterisk,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "*"
            });

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddSubnet_WithNetworkSecurityGroup_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var nsg = vnet.AddNetworkSecurityGroup("web-nsg")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            });

        vnet.AddSubnet("web-subnet", "10.0.1.0/24")
            .WithNetworkSecurityGroup(nsg);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public async Task AddNetworkSecurityGroup_SharedAcrossSubnets_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var nsg = vnet.AddNetworkSecurityGroup("shared-nsg")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            });

        vnet.AddSubnet("subnet1", "10.0.1.0/24")
            .WithNetworkSecurityGroup(nsg);
        vnet.AddSubnet("subnet2", "10.0.2.0/24")
            .WithNetworkSecurityGroup(nsg);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void WithNetworkSecurityGroup_SetsSubnetNetworkSecurityGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");
        var nsg = vnet.AddNetworkSecurityGroup("web-nsg");
        var subnet = vnet.AddSubnet("web-subnet", "10.0.1.0/24")
            .WithNetworkSecurityGroup(nsg);

        Assert.Same(nsg.Resource, subnet.Resource.NetworkSecurityGroup);
    }

    [Fact]
    public void WithSecurityRule_DuplicateName_Throws()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nsg = builder.AddAzureVirtualNetwork("myvnet")
            .AddNetworkSecurityGroup("web-nsg")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            });

        var exception = Assert.Throws<ArgumentException>(() => nsg.WithSecurityRule(new AzureSecurityRule
        {
            Name = "ALLOW-HTTPS",
            Priority = 110,
            Direction = SecurityRuleDirection.Inbound,
            Access = SecurityRuleAccess.Allow,
            Protocol = SecurityRuleProtocol.Tcp,
            SourceAddressPrefix = "*",
            SourcePortRange = "*",
            DestinationAddressPrefix = "*",
            DestinationPortRange = "443"
        }));

        Assert.Contains("allow-https", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MultipleNSGs_WithSameRuleName_GeneratesDistinctBicepIdentifiers()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet = builder.AddAzureVirtualNetwork("myvnet");

        var nsg1 = vnet.AddNetworkSecurityGroup("nsg-one")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "*",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            });

        var nsg2 = vnet.AddNetworkSecurityGroup("nsg-two")
            .WithSecurityRule(new AzureSecurityRule
            {
                Name = "allow-https",
                Priority = 100,
                Direction = SecurityRuleDirection.Inbound,
                Access = SecurityRuleAccess.Allow,
                Protocol = SecurityRuleProtocol.Tcp,
                SourceAddressPrefix = "VirtualNetwork",
                SourcePortRange = "*",
                DestinationAddressPrefix = "*",
                DestinationPortRange = "443"
            });

        vnet.AddSubnet("subnet1", "10.0.1.0/24")
            .WithNetworkSecurityGroup(nsg1);
        vnet.AddSubnet("subnet2", "10.0.2.0/24")
            .WithNetworkSecurityGroup(nsg2);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(vnet.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }

    [Fact]
    public void WithNetworkSecurityGroup_DifferentVNet_Throws()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var vnet1 = builder.AddAzureVirtualNetwork("vnet1");
        var vnet2 = builder.AddAzureVirtualNetwork("vnet2");

        var nsg = vnet1.AddNetworkSecurityGroup("web-nsg");
        var subnet = vnet2.AddSubnet("web-subnet", "10.0.1.0/24");

        var exception = Assert.Throws<ArgumentException>(() => subnet.WithNetworkSecurityGroup(nsg));

        Assert.Contains("vnet1", exception.Message);
        Assert.Contains("vnet2", exception.Message);
    }
}
