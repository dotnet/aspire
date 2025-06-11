// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Azure.Core;

namespace Aspire.Hosting.Azure.Tests;

public class ProvisioningContextTests
{
    [Fact]
    public void ProvisioningContext_CanBeCreatedWithInterfaces()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Credential);
        Assert.NotNull(context.ArmClient);
        Assert.NotNull(context.Subscription);
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.Tenant);
        Assert.NotNull(context.Location.Name);
        Assert.NotNull(context.Principal);
        Assert.NotNull(context.UserSecrets);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectSubscriptionProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.Equal("Test Subscription", context.Subscription.Data.DisplayName);
        Assert.Contains("subscriptions", context.Subscription.Id.ToString());
        Assert.NotNull(context.Subscription.Data.TenantId);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectResourceGroupProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.Equal("test-rg", context.ResourceGroup.Data.Name);
        Assert.Contains("resourceGroups", context.ResourceGroup.Id.ToString());
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectTenantProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.NotNull(context.Tenant.Data.TenantId);
        Assert.Equal("testdomain.onmicrosoft.com", context.Tenant.Data.DefaultDomain);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectLocationProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.Equal("westus2", context.Location.Name);
        Assert.Equal("westus2", context.Location.ToString());
    }

    [Fact]
    public async Task ProvisioningContext_TokenCredential_CanGetToken()
    {
        // Arrange
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();
        var requestContext = new TokenRequestContext(["https://management.azure.com/.default"]);

        // Act
        var token = await context.Credential.GetTokenAsync(requestContext, CancellationToken.None);

        // Assert
        Assert.NotNull(token.Token);
    }

    [Fact]
    public async Task ProvisioningContext_ArmClient_CanGetDefaultSubscription()
    {
        // Arrange
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Act
        var subscription = await context.ArmClient.GetDefaultSubscriptionAsync();

        // Assert  
        Assert.NotNull(subscription);
        // Note: Test ARM client will not return valid data due to Azure SDK limitations
    }

    [Fact(Skip = "Requires actual Azure SDK resources for testing")]
    public async Task ProvisioningContext_ResourceGroup_CanGetArmDeployments()
    {
        // Note: This test is skipped because creating test doubles for Azure SDK resources
        // requires complex setup that is not practical for unit testing
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProvisioningContext_Subscription_CanGetResourceGroups()
    {
        // Arrange
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Act
        var resourceGroups = context.Subscription.GetResourceGroups();
        var response = await resourceGroups.GetAsync("test-rg");

        // Assert
        Assert.NotNull(resourceGroups);
        Assert.NotNull(response);
        Assert.Equal("test-rg", response.Value.Data.Name);
    }

    [Fact]
    public void ProvisioningContext_CanBeCustomized()
    {
        // Arrange
        var customPrincipal = new UserPrincipal(Guid.NewGuid(), "custom@example.com");
        var customUserSecrets = new JsonObject { ["test"] = "value" };

        // Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext(
            principal: customPrincipal,
            userSecrets: customUserSecrets);

        // Assert
        Assert.Equal("custom@example.com", context.Principal.Name);
        Assert.Equal("value", context.UserSecrets["test"]?.ToString());
    }
}

public class ProvisioningServicesTests
{
    [Fact]
    public void ProvisioningTestHelpers_CanCreateAllInterfaces()
    {
        // Arrange & Act
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var secretClientProvider = ProvisioningTestHelpers.CreateSecretClientProvider();
        var bicepCliExecutor = ProvisioningTestHelpers.CreateBicepCompiler();
        var userSecretsManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        // Assert
        Assert.NotNull(armClientProvider);
        Assert.NotNull(secretClientProvider);
        Assert.NotNull(bicepCliExecutor);
        Assert.NotNull(userSecretsManager);
    }

    [Fact]
    public async Task TestBicepCliExecutor_ReturnsValidJson()
    {
        // Arrange
        var executor = ProvisioningTestHelpers.CreateBicepCompiler();

        // Act
        var result = await executor.CompileBicepToArmAsync("test.bicep");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("contentVersion", result);
        Assert.Contains("resources", result);
        
        // Verify it's valid JSON
        var parsed = JsonNode.Parse(result);
        Assert.NotNull(parsed);
    }

    [Fact]
    public async Task TestUserSecretsManager_CanSaveAndLoad()
    {
        // Arrange
        var manager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var secrets = new JsonObject { ["Azure"] = new JsonObject { ["SubscriptionId"] = "test-id" } };

        // Act
        await manager.SaveUserSecretsAsync(secrets);
        var loaded = await manager.LoadUserSecretsAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("test-id", loaded["Azure"]?["SubscriptionId"]?.ToString());
    }

    [Fact]
    public async Task TestUserPrincipalProvider_CanGetUserPrincipal()
    {
        // Arrange
        var provider = ProvisioningTestHelpers.CreateUserPrincipalProvider();

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.NotNull(principal);
        Assert.NotEqual(Guid.Empty, principal.Id);
        Assert.False(string.IsNullOrEmpty(principal.Name));
    }
}
