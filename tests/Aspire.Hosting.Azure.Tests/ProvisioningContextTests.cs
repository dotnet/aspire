// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Azure;
using Azure.Core;
using Azure.ResourceManager.Resources.Models;

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
        Assert.NotNull(context.DeploymentState);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectSubscriptionProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.Equal("Test Subscription", context.Subscription.DisplayName);
        Assert.Contains("subscriptions", context.Subscription.Id.ToString());
        Assert.NotNull(context.Subscription.TenantId);
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectResourceGroupProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.Equal("test-rg", context.ResourceGroup.Name);
        Assert.Contains("resourceGroups", context.ResourceGroup.Id.ToString());
    }

    [Fact]
    public void ProvisioningContext_ExposesCorrectTenantProperties()
    {
        // Arrange & Act
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Assert
        Assert.NotNull(context.Tenant.TenantId);
        Assert.Equal("testdomain.onmicrosoft.com", context.Tenant.DefaultDomain);
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
    public async Task ProvisioningContext_ArmClient_CanGetSubscriptionAndTenant()
    {
        // Arrange
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Act
        var (subscription, tenant) = await context.ArmClient.GetSubscriptionAndTenantAsync();

        // Assert
        Assert.NotNull(subscription);
        Assert.Equal("Test Subscription", subscription.DisplayName);
        Assert.NotNull(tenant);
        Assert.NotNull(tenant.TenantId);
    }

    [Fact]
    public async Task ProvisioningContext_ResourceGroup_CanGetArmDeployments()
    {
        // Arrange
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext();

        // Act
        var deployments = context.ResourceGroup.GetArmDeployments();
        var operation = await deployments.CreateOrUpdateAsync(
            WaitUntil.Started,
            "test-deployment",
            new ArmDeploymentContent(
                new ArmDeploymentProperties(ArmDeploymentMode.Incremental)));

        // Assert
        Assert.NotNull(deployments);
        Assert.NotNull(operation);
        Assert.True(operation.HasCompleted);
        Assert.True(operation.HasValue);
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
        Assert.Equal("test-rg", response.Value.Name);
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
        Assert.Equal("value", context.DeploymentState["test"]?.ToString());
    }

    [Fact]
    public async Task WithDeploymentState_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var deploymentState = new JsonObject();
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext(userSecrets: deploymentState);
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var tasks = new Task[threadCount];

        // Act - Multiple threads accessing the DeploymentState concurrently via WithDeploymentState
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    context.WithDeploymentState(state =>
                    {
                        // All threads try to get or create the same "Azure" property
                        var azureNode = state.Prop("Azure");
                        
                        // Each thread creates a unique property
                        var threadNode = azureNode.Prop($"Thread{threadId}");
                        threadNode.AsObject()["Counter"] = j;
                        
                        // And a shared property under Azure
                        var deploymentsNode = azureNode.Prop("Deployments");
                        
                        // Access a deeper nested property
                        var resourceNode = deploymentsNode.Prop($"Resource{j % 5}");
                        resourceNode.AsObject()["LastAccess"] = $"Thread{threadId}-{j}";
                    });
                }
            });
        }

        // Assert - Should complete without exceptions
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        // Verify the structure was created correctly
        context.WithDeploymentState(state =>
        {
            Assert.NotNull(state["Azure"]);
            var azureObj = state["Azure"]!.AsObject();
            Assert.NotNull(azureObj["Deployments"]);
            
            // Check that all thread-specific nodes were created
            for (int i = 0; i < threadCount; i++)
            {
                Assert.NotNull(azureObj[$"Thread{i}"]);
            }
        });
    }

    [Fact]
    public void WithDeploymentState_Action_ExecutesSuccessfully()
    {
        // Arrange
        var deploymentState = new JsonObject();
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext(userSecrets: deploymentState);

        // Act
        var executed = false;
        context.WithDeploymentState(state =>
        {
            state["TestKey"] = "TestValue";
            executed = true;
        });

        // Assert
        Assert.True(executed);
        Assert.Equal("TestValue", deploymentState["TestKey"]!.GetValue<string>());
    }

    [Fact]
    public void WithDeploymentState_Func_ReturnsValue()
    {
        // Arrange
        var deploymentState = new JsonObject();
        deploymentState["TestKey"] = "TestValue";
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext(userSecrets: deploymentState);

        // Act
        var result = context.WithDeploymentState(state =>
        {
            return state["TestKey"]!.GetValue<string>();
        });

        // Assert
        Assert.Equal("TestValue", result);
    }

    [Fact]
    public async Task WithDeploymentState_ConcurrentReadsAndWrites_MaintainsConsistency()
    {
        // Arrange
        var deploymentState = new JsonObject();
        var context = ProvisioningTestHelpers.CreateTestProvisioningContext(userSecrets: deploymentState);
        const int writerCount = 5;
        const int readerCount = 5;
        const int iterations = 100;

        // Initialize counter
        context.WithDeploymentState(state =>
        {
            state["Counter"] = 0;
        });

        var writerTasks = new Task[writerCount];
        var readerTasks = new Task[readerCount];

        // Act - Writers increment counter
        for (int i = 0; i < writerCount; i++)
        {
            writerTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    context.WithDeploymentState(state =>
                    {
                        var current = state["Counter"]!.GetValue<int>();
                        state["Counter"] = current + 1;
                    });
                }
            });
        }

        // Readers read counter
        var readValues = new List<int>[readerCount];
        for (int i = 0; i < readerCount; i++)
        {
            int readerIndex = i;
            readValues[readerIndex] = new List<int>();
            readerTasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var value = context.WithDeploymentState(state =>
                    {
                        return state["Counter"]!.GetValue<int>();
                    });
                    readValues[readerIndex].Add(value);
                    Thread.Sleep(1); // Small delay to allow interleaving
                }
            });
        }

        await Task.WhenAll(writerTasks.Concat(readerTasks)).WaitAsync(TimeSpan.FromSeconds(15));

        // Assert - Final counter value should be exactly writerCount * iterations
        var finalValue = context.WithDeploymentState(state =>
        {
            return state["Counter"]!.GetValue<int>();
        });

        Assert.Equal(writerCount * iterations, finalValue);

        // All read values should be in valid range (0 to finalValue)
        foreach (var readerValues in readValues)
        {
            Assert.All(readerValues, value =>
            {
                Assert.InRange(value, 0, finalValue);
            });
        }
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
        await manager.SaveStateAsync(secrets);
        var loaded = await manager.LoadStateAsync();

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
