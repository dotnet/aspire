// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
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

        Assert.True(serviceBus.Resource.TryGetExistingAzureResourceAnnotation(out var existingAzureResourceAnnotation));
        Assert.Equal("name", existingAzureResourceAnnotation.NameParameter.Name);
        Assert.Equal("resourceGroup", existingAzureResourceAnnotation.ResourceGroupParameter!.Name);
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

        Assert.True(serviceBus.Resource.TryGetExistingAzureResourceAnnotation(out var existingAzureResourceAnnotation));
        Assert.Equal("name1", existingAzureResourceAnnotation.NameParameter.Name);
        Assert.Equal("resourceGroup1", existingAzureResourceAnnotation.ResourceGroupParameter!.Name);
    }

    [Fact]
    public void PublishAsExistingInPublishModeWorks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var nameParameter = builder.AddParameter("name", "existingName");
        var resourceGroupParameter = builder.AddParameter("resourceGroup", "existingResourceGroup");

        var serviceBus = builder.AddAzureServiceBus("sb")
            .PublishAsExisting(nameParameter, resourceGroupParameter);

        Assert.True(serviceBus.Resource.TryGetExistingAzureResourceAnnotation(out var existingAzureResourceAnnotation));
        Assert.Equal("name", existingAzureResourceAnnotation.NameParameter.Name);
        Assert.Equal("resourceGroup", existingAzureResourceAnnotation.ResourceGroupParameter!.Name);
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

        Assert.True(serviceBus.Resource.TryGetExistingAzureResourceAnnotation(out var existingAzureResourceAnnotation));
        Assert.Equal("name1", existingAzureResourceAnnotation.NameParameter.Name);
        Assert.Equal("resourceGroup1", existingAzureResourceAnnotation.ResourceGroupParameter!.Name);
    }
}
