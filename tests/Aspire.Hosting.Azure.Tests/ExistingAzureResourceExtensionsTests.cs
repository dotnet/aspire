// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class ExistingAzureExtensionsResourceTests
{
    [Fact]
    public void RunAsExistingInPublishModeNoOps()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter);

        Assert.False(serviceBus.Resource.IsExisting());
    }

    [Fact]
    public void RunAsExistingInRunModeWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter);

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.Name);
        Assert.Equal("name", existingNameParameter.Resource.Name);
        var existingResourceGroupParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.Equal("resourceGroup", existingResourceGroupParameter.Resource.Name);
    }

    [Fact]
    public void MultipleRunAsExistingInRunModeUsesLast()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");
        var nameParameter1 = builder.AddParameter("name1", "existingName");
        var resourceGroupParameter1 = builder.AddParameter("resourceGroup1", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .RunAsExisting(nameParameter, resourceGroupParameter)
            .RunAsExisting(nameParameter1, resourceGroupParameter1);

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.Name);
        Assert.Equal("name1", existingNameParameter.Resource.Name);
        var existingResourceGroupParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.Equal("resourceGroup1", existingResourceGroupParameter.Resource.Name);
    }

    [Fact]
    public void PublishAsExistingInPublishModeWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .PublishAsExisting(nameParameter, resourceGroupParameter);

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.Name);
        Assert.Equal("name", existingNameParameter.Resource.Name);
        var existingResourceGroupParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.Equal("resourceGroup", existingResourceGroupParameter.Resource.Name);
    }

    [Fact]
    public void MultiplePublishAsExistingInRunModeUsesLast()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");
        var nameParameter1 = builder.AddParameter("name1", "existingName");
        var resourceGroupParameter1 = builder.AddParameter("resourceGroup1", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .PublishAsExisting(nameParameter, resourceGroupParameter)
            .PublishAsExisting(nameParameter1, resourceGroupParameter1);

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        var existingNameParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.Name);
        Assert.Equal("name1", existingNameParameter.Resource.Name);
        var existingResourceGroupParameter = Assert.IsAssignableFrom<IResourceBuilder<ParameterResource>>(existingAzureResourceAnnotation.ResourceGroup);
        Assert.Equal("resourceGroup1", existingResourceGroupParameter.Resource.Name);
    }

    public static TheoryData<Func<string, string, string, IResourceBuilder<IAzureResource>>> AsExistingMethodsWithString =>
        new()
        {
            { (name, resourceGroup, type) => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).AddAzureServiceBus(type).RunAsExisting(name, resourceGroup) },
            { (name, resourceGroup, type) => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).AddAzureServiceBus(type).PublishAsExisting(name, resourceGroup) }
        };

    [Theory]
    [MemberData(nameof(AsExistingMethodsWithString))]
    public void CanCallAsExistingWithStringArguments(Func<string, string, string, IResourceBuilder<IAzureResource>> runAsExisting)
    {
        var serviceBus = runAsExisting("existingName", "existingResourceGroup", "sb");

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var existingAzureResourceAnnotation));
        Assert.Equal("existingName", existingAzureResourceAnnotation.Name);
        Assert.Equal("existingResourceGroup", existingAzureResourceAnnotation.ResourceGroup);
    }

    public static TheoryData<Func<IResourceBuilder<IAzureResource>>> AsExistingMethodsWithInvalidArguments =>
        new()
        {
            { () => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).AddAzureServiceBus("sb").RunAsExisting(1, 2) },
            { () => TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish).AddAzureServiceBus("sb").PublishAsExisting(1, 2) }
        };

    [Theory]
    [MemberData(nameof(AsExistingMethodsWithInvalidArguments))]
    public void ThrowsForAsExistingWithInvalidArgumentType(Func<IResourceBuilder<IAzureResource>> asExisting)
    {
        Assert.Throws<ArgumentException>(() => asExisting());
    }
}
