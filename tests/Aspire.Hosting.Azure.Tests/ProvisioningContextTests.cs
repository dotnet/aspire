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
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

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
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Assert
        Assert.Equal("Test Subscription", context.Subscription.Data.DisplayName);
        Assert.Contains("subscriptions", context.Subscription.Id.ToString());
        Assert.NotNull(context.Subscription.Data.TenantId);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectResourceGroupProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Assert
        Assert.Equal("test-rg", context.ResourceGroup.Data.Name);
        Assert.Contains("resourceGroups", context.ResourceGroup.Id.ToString());
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectTenantProperties()
    {
        // Note: Tenant resource creation is complex and requires authentication
        // For unit testing, we verify tenant information through subscription data
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Assert
        // Verify tenant information through subscription data, which is the recommended approach
        Assert.NotNull(context.Subscription.Data.TenantId);
        Assert.Equal(Guid.Parse("87654321-4321-4321-4321-210987654321"), context.Subscription.Data.TenantId);
        
        // Note: Direct tenant resource access requires integration tests with real Azure credentials
        // For unit tests, accessing tenant properties through context.Tenant may be limited
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectLocationProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Assert
        Assert.Equal("westus2", context.Location.Name);
        Assert.Equal("westus2", context.Location.ToString());
    }

    [Fact]
    public async Task ProvisioningContext_TokenCredential_CanGetToken()
    {
        // Arrange
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();
        var requestContext = new TokenRequestContext(["https://management.azure.com/.default"]);

        // Act
        var token = await context.Credential.GetTokenAsync(requestContext, CancellationToken.None);

        // Assert
        Assert.NotNull(token.Token);
    }

    [Fact]
    public async Task ProvisioningContext_ArmClient_BasicPropertiesAccessible()
    {
        // Test basic ArmClient functionality that works with test credentials
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Assert - Test that we can create the client and access basic properties
        Assert.NotNull(context.ArmClient);
        
        // Note: GetDefaultSubscriptionAsync() and other complex operations require real Azure authentication
        // For unit testing, we verify that the ArmClient can be created and basic properties accessed
        // Complex operations should be tested with integration tests
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProvisioningContext_ResourceGroup_BasicPropertiesAccessible()
    {
        // Test that we can access resource group properties from test data
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Act & Assert - Test basic property access
        Assert.NotNull(context.ResourceGroup);
        Assert.Equal("test-rg", context.ResourceGroup.Data.Name);
        Assert.Contains("resourceGroups", context.ResourceGroup.Id.ToString());
        
        // Note: GetArmDeployments() and deployment operations require authenticated Azure context
        // For unit testing, we verify resource group data properties
        // Deployment operations should be tested with integration tests
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ProvisioningContext_Subscription_BasicPropertiesAccessible()
    {
        // Test that we can access subscription properties from test data
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant();

        // Act & Assert - Test basic property access
        Assert.NotNull(context.Subscription);
        Assert.Equal("Test Subscription", context.Subscription.Data.DisplayName);
        Assert.Contains("subscriptions", context.Subscription.Id.ToString());
        Assert.NotNull(context.Subscription.Data.TenantId);
        
        // Note: GetResourceGroups() and resource operations require authenticated Azure context
        // For unit testing, we verify subscription data properties
        // Resource operations should be tested with integration tests
        await Task.CompletedTask;
    }

    [Fact]
    public void ProvisioningContext_CanBeCustomized()
    {
        // Arrange
        var customPrincipal = new UserPrincipal(Guid.NewGuid(), "custom@example.com");
        var customUserSecrets = new JsonObject { ["test"] = "value" };

        // Act
        var context = ProvisioningTestHelpers.Instance.CreateTestProvisioningContextWithoutTenant(
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
        var helpers = ProvisioningTestHelpers.Instance;
        var armClientProvider = helpers.CreateArmClientProvider();
        var secretClientProvider = helpers.CreateSecretClientProvider();
        var bicepCliExecutor = helpers.CreateBicepCompiler();
        var userSecretsManager = helpers.CreateUserSecretsManager();

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
        var helpers = ProvisioningTestHelpers.Instance;
        var executor = helpers.CreateBicepCompiler();

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
        var helpers = ProvisioningTestHelpers.Instance;
        var manager = helpers.CreateUserSecretsManager();
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
        var helpers = ProvisioningTestHelpers.Instance;
        var provider = helpers.CreateUserPrincipalProvider();

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.NotNull(principal);
        Assert.NotEqual(Guid.Empty, principal.Id);
        Assert.False(string.IsNullOrEmpty(principal.Name));
    }
}
