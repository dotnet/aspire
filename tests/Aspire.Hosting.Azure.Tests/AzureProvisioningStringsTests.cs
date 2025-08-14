// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure.Resources;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisioningStringsTests
{
    [Fact]
    public void AzureProvisioningStrings_AllResourcesLoadCorrectly()
    {
        // Act & Assert - Verify that all resource strings can be loaded
        Assert.NotNull(AzureProvisioningStrings.NotificationTitle);
        Assert.NotEmpty(AzureProvisioningStrings.NotificationTitle);
        Assert.Equal("Azure provisioning", AzureProvisioningStrings.NotificationTitle);

        Assert.NotNull(AzureProvisioningStrings.NotificationMessage);
        Assert.NotEmpty(AzureProvisioningStrings.NotificationMessage);
        Assert.Equal("The model contains Azure resources that require an Azure Subscription.", AzureProvisioningStrings.NotificationMessage);

        Assert.NotNull(AzureProvisioningStrings.NotificationPrimaryButtonText);
        Assert.NotEmpty(AzureProvisioningStrings.NotificationPrimaryButtonText);
        Assert.Equal("Enter values", AzureProvisioningStrings.NotificationPrimaryButtonText);

        Assert.NotNull(AzureProvisioningStrings.InputsTitle);
        Assert.NotEmpty(AzureProvisioningStrings.InputsTitle);
        Assert.Equal("Azure provisioning", AzureProvisioningStrings.InputsTitle);

        Assert.NotNull(AzureProvisioningStrings.InputsMessage);
        Assert.NotEmpty(AzureProvisioningStrings.InputsMessage);
        Assert.Contains("The model contains Azure resources that require an Azure Subscription.", AzureProvisioningStrings.InputsMessage);
        Assert.Contains("Azure provisioning docs", AzureProvisioningStrings.InputsMessage);

        Assert.NotNull(AzureProvisioningStrings.LocationLabel);
        Assert.NotEmpty(AzureProvisioningStrings.LocationLabel);
        Assert.Equal("Location", AzureProvisioningStrings.LocationLabel);

        Assert.NotNull(AzureProvisioningStrings.LocationPlaceholder);
        Assert.NotEmpty(AzureProvisioningStrings.LocationPlaceholder);
        Assert.Equal("Select location", AzureProvisioningStrings.LocationPlaceholder);

        Assert.NotNull(AzureProvisioningStrings.SubscriptionIdLabel);
        Assert.NotEmpty(AzureProvisioningStrings.SubscriptionIdLabel);
        Assert.Equal("Subscription ID", AzureProvisioningStrings.SubscriptionIdLabel);

        Assert.NotNull(AzureProvisioningStrings.SubscriptionIdPlaceholder);
        Assert.NotEmpty(AzureProvisioningStrings.SubscriptionIdPlaceholder);
        Assert.Equal("Select subscription ID", AzureProvisioningStrings.SubscriptionIdPlaceholder);

        Assert.NotNull(AzureProvisioningStrings.ResourceGroupLabel);
        Assert.NotEmpty(AzureProvisioningStrings.ResourceGroupLabel);
        Assert.Equal("Resource group", AzureProvisioningStrings.ResourceGroupLabel);

        Assert.NotNull(AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
        Assert.NotEmpty(AzureProvisioningStrings.ValidationSubscriptionIdInvalid);
        Assert.Equal("Subscription ID must be a valid GUID.", AzureProvisioningStrings.ValidationSubscriptionIdInvalid);

        Assert.NotNull(AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
        Assert.NotEmpty(AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
        Assert.Equal("Resource group name must be a valid Azure resource group name.", AzureProvisioningStrings.ValidationResourceGroupNameInvalid);
    }

    [Fact]
    public void AzureProvisioningStrings_ResourceManager_IsNotNull()
    {
        // Act & Assert - Verify that the ResourceManager is properly initialized
        Assert.NotNull(AzureProvisioningStrings.ResourceManager);
    }
}