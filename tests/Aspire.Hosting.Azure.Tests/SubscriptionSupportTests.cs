// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class SubscriptionSupportTests
{
    [Fact]
    public void ExistingAzureResourceAnnotation_CanHaveSubscription()
    {
        // Test the new constructor with subscription parameter
        var annotation = new ExistingAzureResourceAnnotation("myResource", "myResourceGroup", "12345678-1234-1234-1234-123456789012");
        
        Assert.Equal("myResource", annotation.Name);
        Assert.Equal("myResourceGroup", annotation.ResourceGroup);
        Assert.Equal("12345678-1234-1234-1234-123456789012", annotation.Subscription);
    }

    [Fact]
    public void ExistingAzureResourceAnnotation_WithoutSubscription_HasNullSubscription()
    {
        // Test the original constructor still works
        var annotation = new ExistingAzureResourceAnnotation("myResource", "myResourceGroup");
        
        Assert.Equal("myResource", annotation.Name);
        Assert.Equal("myResourceGroup", annotation.ResourceGroup);
        Assert.Null(annotation.Subscription);
    }

    [Fact]
    public void AsExistingWithStringAndSubscription_SetsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var serviceBus = builder.AddAzureServiceBus("sb")
            .AsExisting("existingName", "existingResourceGroup", "12345678-1234-1234-1234-123456789012");

        Assert.True(serviceBus.Resource.TryGetLastAnnotation<ExistingAzureResourceAnnotation>(out var annotation));
        Assert.Equal("existingName", annotation.Name);
        Assert.Equal("existingResourceGroup", annotation.ResourceGroup);
        Assert.Equal("12345678-1234-1234-1234-123456789012", annotation.Subscription);
    }
}