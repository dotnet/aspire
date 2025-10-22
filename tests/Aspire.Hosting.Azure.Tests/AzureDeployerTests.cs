// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Utils;
using Aspire.Hosting.Tests;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Publishing.Internal;
using Aspire.Hosting.Testing;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Aspire.TestUtilities;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Azure.Tests;

public class AzureDeployerTests(ITestOutputHelper output)
{
    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11105")]
    public void DeployAsync_DoesNotEmitPublishedResources()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-deployer-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName, isDeploy: true);
        // Configure Azure settings to avoid prompting during deployment for this test case
        ConfigureTestServices(builder, bicepProvisioner: new NoOpBicepProvisioner());

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Act
        using var app = builder.Build();
        app.Run();

        // Assert files exist but don't verify contents
        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.False(File.Exists(mainBicepPath));
        var envBicepPath = Path.Combine(tempDir.FullName, "env", "env.bicep");
        Assert.False(File.Exists(envBicepPath));

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task DeployAsync_PromptsViaInteractionService()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner(), setDefaultProvisioningOptions: false);

        // Add an Azure environment resource which will trigger the deployment prompting
        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();

        var runTask = Task.Run(app.Run);

        // Wait for the first interaction (subscription selection)
        var tenantInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure tenant", tenantInteraction.Title);
        Assert.False(tenantInteraction.Options!.EnableMessageMarkdown);

        Assert.Collection(tenantInteraction.Inputs,
            input =>
            {
                Assert.Equal("Tenant ID", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            });

        tenantInteraction.Inputs[0].Value = "87654321-4321-4321-4321-210987654321";
        tenantInteraction.CompletionTcs.SetResult(InteractionResult.Ok(tenantInteraction.Inputs));

        // Wait for the next interaction (subscription selection)
        var subscriptionInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure subscription", subscriptionInteraction.Title);
        Assert.False(subscriptionInteraction.Options!.EnableMessageMarkdown);

        // Verify the expected input for subscription selection (fallback to manual entry)
        Assert.Collection(subscriptionInteraction.Inputs,
            input =>
            {
                Assert.Equal("Subscription ID", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            });

        // Complete the subscription interaction
        subscriptionInteraction.Inputs[0].Value = "12345678-1234-1234-1234-123456789012";
        subscriptionInteraction.CompletionTcs.SetResult(InteractionResult.Ok(subscriptionInteraction.Inputs));

        // Wait for the second interaction (location and resource group selection)
        var locationInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure location and resource group", locationInteraction.Title);
        Assert.False(locationInteraction.Options!.EnableMessageMarkdown);

        // Verify the expected inputs for location and resource group (fallback to manual entry)
        Assert.Collection(locationInteraction.Inputs,
            input =>
            {
                Assert.Equal("Location", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal("Resource group", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.False(input.Required);
            });

        // Complete the location interaction
        locationInteraction.Inputs[0].Value = "westus2";
        locationInteraction.Inputs[1].Value = "test-rg";
        locationInteraction.CompletionTcs.SetResult(InteractionResult.Ok(locationInteraction.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task DeployAsync_WithAzureStorageResourcesWorks()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();

        // Add Azure Storage with blob containers and queues
        var storage = builder.AddAzureStorage("teststorage");
        storage.AddBlobContainer("container1", blobContainerName: "test-container-1");
        storage.AddBlobContainer("container2", blobContainerName: "test-container-2");
        storage.AddQueue("testqueue", queueName: "test-queue");

        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that ACR login command was not executed given no compute resources
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify MockImageBuilder was NOT called when there are no compute resources
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.False(mockImageBuilder.BuildImageCalled);
        Assert.False(mockImageBuilder.BuildImagesCalled);
        Assert.False(mockImageBuilder.TagImageCalled);
        Assert.False(mockImageBuilder.PushImageCalled);
        Assert.Empty(mockImageBuilder.BuildImageResources);
        Assert.Empty(mockImageBuilder.TagImageCalls);
        Assert.Empty(mockImageBuilder.PushImageCalls);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithContainer_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();
        builder.AddContainer("api", "my-api-image:latest");

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated to outputs because they are
        // hoisted up for the container resource
        Assert.Equal("testregistry", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("testregistry.azurecr.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert - Verify ACR login command was not called since no image was pushed
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify MockImageBuilder tag and push methods were NOT called for existing container image
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.False(mockImageBuilder.TagImageCalled);
        Assert.False(mockImageBuilder.PushImageCalled);
        Assert.Empty(mockImageBuilder.TagImageCalls);
        Assert.Empty(mockImageBuilder.PushImageCalls);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithDockerfile_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();
        builder.AddDockerfile("api", "api.Dockerfile");

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated to outputs because they are
        // hoisted up for the container resource
        Assert.Equal("testregistry", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("testregistry.azurecr.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert - Verify ACR login command was called since Dockerfile image needs to be pushed
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify MockImageBuilder tag and push methods were called
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.True(mockImageBuilder.TagImageCalled);
        Assert.True(mockImageBuilder.PushImageCalled);

        // Verify specific tag call was made (local "api" to target in testregistry with deployment tag)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName.StartsWith("api:") &&
            call.targetImageName.StartsWith("testregistry.azurecr.io/") &&
            call.targetImageName.Contains("aspire-deploy-"));

        // Verify specific push call was made with deployment tag
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/") &&
            imageName.Contains("aspire-deploy-"));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithProjectResource_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();
        builder.AddProject<Project>("api", launchProfileName: null);

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated to outputs because they are
        // hoisted up for the container resource
        Assert.Equal("testregistry", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("testregistry.azurecr.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert - Verify ACR login command was called
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify MockImageBuilder tag and push methods were called
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.True(mockImageBuilder.TagImageCalled);
        Assert.True(mockImageBuilder.PushImageCalled);

        // Verify specific tag call was made (local "api" to target in testregistry with deployment tag)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "api" &&
            call.targetImageName.StartsWith("testregistry.azurecr.io/") &&
            call.targetImageName.Contains("aspire-deploy-"));

        // Verify specific push call was made with deployment tag
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/") &&
            imageName.Contains("aspire-deploy-"));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithMultipleComputeEnvironments_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(deploymentName =>
        {
            return deploymentName switch
            {
                string name when name.StartsWith("aca-env") => new Dictionary<string, object>
                {
                    ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "acaregistry" },
                    ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "acaregistry.azurecr.io" },
                    ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/aca-identity" },
                    ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "aca.westus.azurecontainerapps.io" },
                    ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/acaenv" }
                },
                string name when name.StartsWith("aas-env") => new Dictionary<string, object>
                {
                    ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "aasregistry" },
                    ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "aasregistry.azurecr.io" },
                    ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/aas-identity" },
                    ["planId"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Web/serverfarms/aasplan" },
                    ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID"] = new { type = "String", value = "aas-client-id" }
                },
                _ => []
            };
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner);

        var acaEnv = builder.AddAzureContainerAppEnvironment("aca-env");
        var aasEnv = builder.AddAzureAppServiceEnvironment("aas-env");
        var azureEnv = builder.AddAzureEnvironment();

        var storage = builder.AddAzureStorage("storage");
        storage.AddBlobContainer("mycontainer1", blobContainerName: "test-container-1");
        storage.AddBlobContainer("mycontainer2", blobContainerName: "test-container-2");
        storage.AddQueue("myqueue", queueName: "my-queue");

        builder.AddRedis("cache").WithComputeEnvironment(acaEnv);
        builder.AddProject<Project>("api-service", launchProfileName: null).WithComputeEnvironment(aasEnv);
        builder.AddDockerfile("python-app", "python-app.Dockerfile").WithComputeEnvironment(acaEnv);

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert ACA environment outputs are properly set
        Assert.Equal("acaregistry", acaEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("acaregistry.azurecr.io", acaEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/aca-identity", acaEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("aca.westus.azurecontainerapps.io", acaEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/acaenv", acaEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert AAS environment outputs are properly set
        Assert.Equal("aasregistry", aasEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("aasregistry.azurecr.io", aasEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/aas-identity", aasEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Web/serverfarms/aasplan", aasEnv.Resource.Outputs["planId"]);
        Assert.Equal("aas-client-id", aasEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID"]);

        // Assert ACR login commands were called for both registries
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name acaregistry");
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name aasregistry");

        // Assert - Verify MockImageBuilder tag and push methods were called for multiple registries
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.True(mockImageBuilder.TagImageCalled);
        Assert.True(mockImageBuilder.PushImageCalled);

        // Verify tag calls were made for both registries with deployment tags
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "api-service" &&
            call.targetImageName.StartsWith("aasregistry.azurecr.io/") &&
            call.targetImageName.Contains("aspire-deploy-"));
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName.StartsWith("python-app:") &&
            call.targetImageName.StartsWith("acaregistry.azurecr.io/") &&
            call.targetImageName.Contains("aspire-deploy-"));

        // Verify push calls were made for both registries with deployment tags
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("aasregistry.azurecr.io/") &&
            imageName.Contains("aspire-deploy-"));
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("acaregistry.azurecr.io/") &&
            imageName.Contains("aspire-deploy-"));

        // Verify that redis (existing container) was not tagged/pushed
        Assert.DoesNotContain(mockImageBuilder.TagImageCalls, call => call.localImageName == "cache");
    }

    [Fact]
    public async Task DeployAsync_WithUnresolvedParameters_PromptsForParameterValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());

        // Add a parameter that will be unresolved
        var param = builder.AddParameter("test-param");
        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        var runTask = Task.Run(app.Run);

        // Wait for the parameter inputs interaction (no notification in publish mode)
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the parameter input (should not include save to secrets option in publish mode)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("test-param", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.Equal("Enter value for test-param", input.Placeholder);
            });

        // Complete the parameter inputs interaction
        parameterInputs.Inputs[0].Value = "test-value";
        parameterInputs.CompletionTcs.SetResult(InteractionResult.Ok(parameterInputs.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));

        var setValue = await param.Resource.GetValueAsync(default);
        Assert.Equal("test-value", setValue);
    }

    [Fact]
    public async Task DeployAsync_WithResolvedParameters_SkipsPrompting()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());
        builder.Configuration["Parameters:test-param-2"] = "resolved-value-2";

        // Add a parameter with a resolved value
        var param = builder.AddParameter("test-param", () => "resolved-value");
        var secondParam = builder.AddParameter("test-param-2");
        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        Assert.Equal(0, testInteractionService.Interactions.Reader.Count);
    }

    [Fact]
    public async Task DeployAsync_WithCustomInputGeneratorParameter_RespectsInputGenerator()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());

        // Add a parameter with a custom input generator
        var param = builder.AddParameter("custom-param")
            .WithCustomInput(p => new InteractionInput
            {
                Name = p.Name,
                InputType = InputType.Number,
                Label = "Custom Port Number",
                Description = "Enter a custom port number for the service",
                EnableDescriptionMarkdown = false,
                Placeholder = "8080"
            });
        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        var runTask = Task.Run(app.Run);

        // Wait for the parameter inputs interaction (no notification in publish mode)
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the custom input generator is respected (should not include save to secrets option in publish mode)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("custom-param", input.Name);
                Assert.Equal("Custom Port Number", input.Label);
                Assert.Equal("Enter a custom port number for the service", input.Description);
                Assert.Equal(InputType.Number, input.InputType);
                Assert.Equal("8080", input.Placeholder);
                Assert.False(input.EnableDescriptionMarkdown);
            });

        // Complete the parameter inputs interaction
        parameterInputs.Inputs[0].Value = "9090";
        parameterInputs.CompletionTcs.SetResult(InteractionResult.Ok(parameterInputs.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));

        var setValue = await param.Resource.GetValueAsync(default);
        Assert.Equal("9090", setValue);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithSingleRedisCache_CallsDeployingComputeResources()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        var mockActivityReporter = new TestPublishingActivityReporter();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner, activityReporter: mockActivityReporter);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();

        // Add a single Redis cache resource which is a compute resource
        builder.AddRedis("cache").WithComputeEnvironment(containerAppEnv);

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated
        Assert.Equal("testregistry", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("testregistry.azurecr.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert that compute resources deployment logic was triggered (Redis doesn't require image build/push)
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.False(mockImageBuilder.BuildImageCalled);
        Assert.False(mockImageBuilder.TagImageCalled);
        Assert.False(mockImageBuilder.PushImageCalled);

        // Assert that ACR login was not called since Redis uses existing container image
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert that deploying steps executed
        Assert.Contains("deploy-compute", mockActivityReporter.CreatedSteps);
        Assert.Contains(("deploy-compute", "Deploying **cache**"), mockActivityReporter.CreatedTasks);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithOnlyAzureResources_PrintsDashboardUrl()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        var mockActivityReporter = new TestPublishingActivityReporter();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner, activityReporter: mockActivityReporter);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();

        // Add only Azure resources (no compute resources)
        var storage = builder.AddAzureStorage("teststorage");
        storage.AddBlobContainer("container1", blobContainerName: "test-container-1");

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert that no compute resources were deployed (no image build/push)
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.False(mockImageBuilder.BuildImageCalled);
        Assert.False(mockImageBuilder.TagImageCalled);
        Assert.False(mockImageBuilder.PushImageCalled);

        // Assert that ACR login was not called since no compute resources
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert that the completion request was called
        Assert.True(mockActivityReporter.CompletePublishCalled);
    }

    [Fact]
    public async Task DeployAsync_WithGeneratedParameters_DoesNotPromptsForParameterValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());

        // Add a parameter with GenerateParameterDefault (like Redis password)
        var redis = builder.AddRedis("cache");
        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        Assert.Equal(0, testInteractionService.Interactions.Reader.Count);
    }

    [Fact]
    public async Task DeployAsync_WithParametersInEnvironmentVariables_DiscoversAndPromptsForParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());

        // Create a parameter that will be referenced in environment variables but not added to the model
        var dependentParam = new ParameterResource("dependent-param", p => throw new MissingParameterValueException("Should be prompted"), secret: false);

        // Create a container that references the parameter in its environment variables
        var container = builder.AddContainer("test-container", "test-image")
            .WithEnvironment("DEPENDENT_VALUE", dependentParam);

        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        var runTask = Task.Run(app.Run);

        // Wait for the parameter inputs interaction (no notification in publish mode)
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the dependent parameter is discovered and prompted for (should not include save to secrets option in publish mode)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("dependent-param", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.Equal("Enter value for dependent-param", input.Placeholder);
            });

        // Complete the parameter inputs interaction
        parameterInputs.Inputs[0].Value = "discovered-param-value";
        parameterInputs.CompletionTcs.SetResult(InteractionResult.Ok(parameterInputs.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));

        var setValue = await dependentParam.GetValueAsync(default);
        Assert.Equal("discovered-param-value", setValue);
    }

    [Fact]
    public async Task DeployAsync_WithParametersInArguments_DiscoversAndPromptsForParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var testInteractionService = new TestInteractionService();
        ConfigureTestServices(builder, interactionService: testInteractionService, bicepProvisioner: new NoOpBicepProvisioner());

        // Create a parameter that will be referenced in command line arguments but not added to the model
        var portParam = new ParameterResource("app-port", p => throw new MissingParameterValueException("Should be prompted"), secret: false);

        // Create a container that references the parameter in its command line arguments
        var container = builder.AddContainer("test-container", "test-image")
            .WithArgs("--port", portParam, "--verbose");

        builder.AddAzureEnvironment();

        // Act
        using var app = builder.Build();
        var runTask = Task.Run(app.Run);

        // Wait for the parameter inputs interaction (no notification in publish mode)
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the dependent parameter is discovered and prompted for (should not include save to secrets option in publish mode)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("app-port", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.Equal("Enter value for app-port", input.Placeholder);
            });

        // Complete the parameter inputs interaction
        parameterInputs.Inputs[0].Value = "8080";
        parameterInputs.CompletionTcs.SetResult(InteractionResult.Ok(parameterInputs.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));

        var setValue = await portParam.GetValueAsync(default);
        Assert.Equal("8080", setValue);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11728")]
    public async Task DeployAsync_WithAzureFunctionsProject_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var deploymentOutputsProvider = (string deploymentName) => deploymentName switch
        {
            string name when name.StartsWith("env") => new Dictionary<string, object>
            {
                ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
                ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
                ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
                ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
                ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
            },
            string name when name.StartsWith("funcstorage") => new Dictionary<string, object>
            {
                ["name"] = new { type = "String", value = "testfuncstorage" },
                ["blobEndpoint"] = new { type = "String", value = "https://testfuncstorage.blob.core.windows.net/" },
                ["queueEndpoint"] = new { type = "String", value = "https://testfuncstorage.queue.core.windows.net/" },
                ["tableEndpoint"] = new { type = "String", value = "https://testfuncstorage.table.core.windows.net/" }
            },
            string name when name.StartsWith("hoststorage") => new Dictionary<string, object>
            {
                ["name"] = new { type = "String", value = "testhoststorage" },
                ["blobEndpoint"] = new { type = "String", value = "https://testhoststorage.blob.core.windows.net/" },
                ["queueEndpoint"] = new { type = "String", value = "https://testhoststorage.queue.core.windows.net/" },
                ["tableEndpoint"] = new { type = "String", value = "https://testhoststorage.table.core.windows.net/" }
            },
            string name when name.StartsWith("funcapp-identity") => new Dictionary<string, object>
            {
                ["principalId"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            },
            string name when name.StartsWith("funcapp") => new Dictionary<string, object>
            {
                ["identity_id"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
                ["identity_clientId"] = new { type = "String", value = "test-client-id" }
            },
            _ => []
        };

        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider(deploymentOutputsProvider);
        ConfigureTestServices(builder, armClientProvider: armClientProvider, processRunner: mockProcessRunner);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();

        // Add Azure Storage for the Functions project
        var storage = builder.AddAzureStorage("funcstorage");
        var hostStorage = builder.AddAzureStorage("hoststorage");
        var blobs = storage.AddBlobs("blobs");
        var funcApp = builder.AddAzureFunctionsProject<TestFunctionsProject>("funcapp")
            .WithReference(blobs)
            .WithHostStorage(hostStorage);

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert that container environment outputs are propagated
        Assert.Equal("testregistry", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_NAME"]);
        Assert.Equal("testregistry.azurecr.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_ENDPOINT"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"]);
        Assert.Equal("test.westus.azurecontainerapps.io", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv", containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);

        // Assert that funcapp outputs are propagated
        var funcAppDeployment = Assert.IsAssignableFrom<AzureProvisioningResource>(funcApp.Resource.GetDeploymentTargetAnnotation()?.DeploymentTarget);
        Assert.NotNull(funcAppDeployment);
        Assert.Equal(await ((BicepOutputReference)funcAppDeployment.Parameters["env_outputs_azure_container_apps_environment_default_domain"]!).GetValueAsync(), containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"]);
        Assert.Equal(await ((BicepOutputReference)funcAppDeployment.Parameters["env_outputs_azure_container_apps_environment_id"]!).GetValueAsync(), containerAppEnv.Resource.Outputs["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"]);
        Assert.Equal("https://testfuncstorage.blob.core.windows.net/", await ((BicepOutputReference)funcAppDeployment.Parameters["funcstorage_outputs_blobendpoint"]!).GetValueAsync());
        Assert.Equal("https://testhoststorage.blob.core.windows.net/", await ((BicepOutputReference)funcAppDeployment.Parameters["hoststorage_outputs_blobendpoint"]!).GetValueAsync());

        // Assert - Verify ACR login command was called since Functions image needs to be built and pushed
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify MockImageBuilder tag and push methods were called
        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;
        Assert.NotNull(mockImageBuilder);
        Assert.True(mockImageBuilder.TagImageCalled);
        Assert.True(mockImageBuilder.PushImageCalled);

        // Verify specific tag call was made (local "funcapp" to target in testregistry with deployment tag)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "funcapp" &&
            call.targetImageName.StartsWith("testregistry.azurecr.io/") &&
            call.targetImageName.Contains("aspire-deploy-"));

        // Verify specific push call was made with deployment tag
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/") &&
            imageName.Contains("aspire-deploy-"));
    }

    private static void ConfigureTestServices(IDistributedApplicationTestingBuilder builder,
        IInteractionService? interactionService = null,
        IBicepProvisioner? bicepProvisioner = null,
        IArmClientProvider? armClientProvider = null,
        MockProcessRunner? processRunner = null,
        IPipelineActivityReporter? activityReporter = null,
        bool setDefaultProvisioningOptions = true)
    {
        var options = setDefaultProvisioningOptions ? ProvisioningTestHelpers.CreateOptions() : ProvisioningTestHelpers.CreateOptions(null, null, null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        armClientProvider ??= ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        builder.Services.AddSingleton(armClientProvider);
        builder.Services.AddSingleton(userPrincipalProvider);
        builder.Services.AddSingleton(tokenCredentialProvider);
        builder.Services.AddSingleton(environment);
        builder.Services.AddSingleton(logger);
        builder.Services.AddSingleton(options);
        if (interactionService is not null)
        {
            builder.Services.AddSingleton(interactionService);
        }
        if (activityReporter is not null)
        {
            builder.Services.AddSingleton(activityReporter);
        }
        builder.Services.AddSingleton<IProvisioningContextProvider, PublishModeProvisioningContextProvider>();
        builder.Services.AddSingleton<IDeploymentStateManager, NoOpDeploymentStateManager>();
        if (bicepProvisioner is not null)
        {
            builder.Services.AddSingleton(bicepProvisioner);
        }
        builder.Services.AddSingleton<IProcessRunner>(processRunner ?? new MockProcessRunner());
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();
    }

    private sealed class NoOpDeploymentStateManager : IDeploymentStateManager
    {
        public string? StateFilePath => null;

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
            => Task.FromResult(new DeploymentStateSection(sectionName, [], 0, null));

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpBicepProvisioner : IBicepProvisioner
    {
        public Task<bool> ConfigureResourceAsync(IConfiguration configuration, AzureBicepResource resource, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task GetOrCreateResourceAsync(AzureBicepResource resource, ProvisioningContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact(Skip = "az cli not available on azdo", SkipType = typeof(PlatformDetection), SkipWhen = nameof(PlatformDetection.IsRunningFromAzdo))]
    public async Task DeployAsync_ShowsEndpointOnlyForExternalEndpoints()
    {
        // Arrange
        var activityReporter = new TestPublishingActivityReporter();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var armClientProvider = new TestArmClientProvider(new Dictionary<string, object>
        {
            ["AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        });
        ConfigureTestServices(builder, armClientProvider: armClientProvider, activityReporter: activityReporter);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");
        var azureEnv = builder.AddAzureEnvironment();

        // Add container with external endpoint
        var externalContainer = builder.AddContainer("external-api", "external-image:latest")
            .WithHttpEndpoint(port: 80, name: "http")
            .WithExternalHttpEndpoints();

        // Add container with internal endpoint only
        var internalContainer = builder.AddContainer("internal-api", "internal-image:latest")
            .WithHttpEndpoint(port: 80, name: "http");

        // Add container with no endpoints
        var noEndpointContainer = builder.AddContainer("worker", "worker-image:latest");

        // Act
        using var app = builder.Build();
        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Assert - Verify that external container shows URL in completion message
        var externalTask = activityReporter.CompletedTasks.FirstOrDefault(t => t.TaskStatusText.Contains("external-api"));
        Assert.NotNull(externalTask.CompletionMessage);
        Assert.Contains("https://external-api.test.westus.azurecontainerapps.io", externalTask.CompletionMessage);

        // Assert - Verify that internal container does NOT show URL in completion message
        var internalTask = activityReporter.CompletedTasks.FirstOrDefault(t => t.TaskStatusText.Contains("internal-api"));
        Assert.NotNull(internalTask.CompletionMessage);
        Assert.DoesNotContain("https://", internalTask.CompletionMessage);
        Assert.Equal("Successfully deployed **internal-api**", internalTask.CompletionMessage);

        // Assert - Verify that container with no endpoints does NOT show URL in completion message
        var noEndpointTask = activityReporter.CompletedTasks.FirstOrDefault(t => t.TaskStatusText.Contains("worker"));
        Assert.NotNull(noEndpointTask.CompletionMessage);
        Assert.DoesNotContain("https://", noEndpointTask.CompletionMessage);
        Assert.Equal("Successfully deployed **worker**", noEndpointTask.CompletionMessage);
    }

    private sealed class Project : IProjectMetadata
    {
        public string ProjectPath => "project";
    }

    private sealed class TestFunctionsProject : IProjectMetadata
    {
        public string ProjectPath => "functions-project";

        public LaunchSettings LaunchSettings => new()
        {
            Profiles = new Dictionary<string, LaunchProfile>
            {
                ["funcapp"] = new()
                {
                    CommandLineArgs = "--port 7071",
                    LaunchBrowser = false,
                }
            }
        };
    }

    [Fact]
    public async Task DeployAsync_FirstDeployment_SavesStateToFile()
    {
        var appHostSha = "testsha1first";

        using var builder = TestDistributedApplicationBuilder.Create(
            $"Publishing:Publisher=default",
            $"Publishing:OutputPath=./",
            $"Publishing:Deploy=true",
            $"AppHostSha={appHostSha}");

        ConfigureTestServicesWithFileDeploymentStateManager(builder, bicepProvisioner: new NoOpBicepProvisioner());

        builder.AddAzureEnvironment();

        using var app = builder.Build();

        var deploymentStateManager = app.Services.GetRequiredService<IDeploymentStateManager>();
        var deploymentStatePath = deploymentStateManager.StateFilePath;
        Assert.NotNull(deploymentStatePath);

        if (File.Exists(deploymentStatePath))
        {
            File.Delete(deploymentStatePath);
        }

        await app.StartAsync();
        await app.WaitForShutdownAsync();

        Assert.True(File.Exists(deploymentStatePath));
        var stateContent = await File.ReadAllTextAsync(deploymentStatePath);
        var stateJson = JsonNode.Parse(stateContent);
        Assert.NotNull(stateJson);
        Assert.True(stateJson.AsObject().ContainsKey("Azure:SubscriptionId"));

        if (File.Exists(deploymentStatePath))
        {
            File.Delete(deploymentStatePath);
        }
    }

    [Fact]
    public async Task DeployAsync_WithCachedDeploymentState_LoadsFromCache()
    {
        var appHostSha = "testsha2cache";
        var deploymentStatePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspire",
            "deployments",
            appHostSha,
            $"Production.json"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(deploymentStatePath)!);
        var cachedState = new JsonObject
        {
            ["Azure:SubscriptionId"] = "cached-sub-12345678-1234-1234-1234-123456789012",
            ["Azure:Location"] = "westus2",
            ["Azure:ResourceGroup"] = "cached-rg-test"
        };
        await File.WriteAllTextAsync(deploymentStatePath, cachedState.ToJsonString());

        using var builder = TestDistributedApplicationBuilder.Create(
            $"Publishing:Publisher=default",
            $"Publishing:OutputPath=./",
            $"Publishing:Deploy=true",
            $"AppHostSha={appHostSha}");

        ConfigureTestServicesWithFileDeploymentStateManager(builder, bicepProvisioner: new NoOpBicepProvisioner());
        using var app = builder.Build();

        // Verify that the cached state was loaded into configuration
        Assert.Equal("cached-sub-12345678-1234-1234-1234-123456789012", builder.Configuration["Azure:SubscriptionId"]);
        Assert.Equal("westus2", builder.Configuration["Azure:Location"]);
        Assert.Equal("cached-rg-test", builder.Configuration["Azure:ResourceGroup"]);

        builder.AddAzureEnvironment();

        await app.StartAsync();
        await app.WaitForShutdownAsync();

        // Verify that the state file still exists after deployment
        Assert.True(File.Exists(deploymentStatePath));

        if (File.Exists(deploymentStatePath))
        {
            File.Delete(deploymentStatePath);
        }
    }

    [Fact]
    public async Task DeployAsync_WithClearCacheFlag_DoesNotSaveState()
    {
        var appHostSha = "testsha3clear";

        using var builder = TestDistributedApplicationBuilder.Create(
            $"Publishing:Publisher=default",
            $"Publishing:OutputPath=./",
            $"Publishing:Deploy=true",
            $"Publishing:ClearCache=true",
            $"AppHostSha={appHostSha}");

        ConfigureTestServicesWithFileDeploymentStateManager(builder, bicepProvisioner: new NoOpBicepProvisioner());

        builder.AddAzureEnvironment();

        using var app = builder.Build();

        var deploymentStateManager = app.Services.GetRequiredService<IDeploymentStateManager>();
        var deploymentStatePath = deploymentStateManager.StateFilePath;
        Assert.NotNull(deploymentStatePath);

        if (File.Exists(deploymentStatePath))
        {
            File.Delete(deploymentStatePath);
        }

        await app.StartAsync();
        await app.WaitForShutdownAsync();

        Assert.False(File.Exists(deploymentStatePath));
    }

    [Fact]
    public async Task DeployAsync_WithStagingEnvironment_UsesStagingStateFile()
    {
        var appHostSha = "testsha4stage";

        using var builder = TestDistributedApplicationBuilder.Create(
            $"Publishing:Publisher=default",
            $"Publishing:OutputPath=./",
            $"Publishing:Deploy=true",
            $"AppHostSha={appHostSha}");

        ConfigureTestServicesWithFileDeploymentStateManager(builder, bicepProvisioner: new NoOpBicepProvisioner(), environmentName: "Staging");

        builder.AddAzureEnvironment();

        using var app = builder.Build();

        var deploymentStateManager = app.Services.GetRequiredService<IDeploymentStateManager>();
        var stagingStatePath = deploymentStateManager.StateFilePath;
        Assert.NotNull(stagingStatePath);
        Assert.EndsWith("staging.json", stagingStatePath);

        if (File.Exists(stagingStatePath))
        {
            File.Delete(stagingStatePath);
        }

        await app.StartAsync();
        await app.WaitForShutdownAsync();

        Assert.True(File.Exists(stagingStatePath));
        var stateContent = await File.ReadAllTextAsync(stagingStatePath);
        var stateJson = JsonNode.Parse(stateContent);
        Assert.NotNull(stateJson);

        if (File.Exists(stagingStatePath))
        {
            File.Delete(stagingStatePath);
        }
    }

    private static void ConfigureTestServicesWithFileDeploymentStateManager(
        IDistributedApplicationTestingBuilder builder,
        IBicepProvisioner? bicepProvisioner = null,
        string? environmentName = null)
    {
        var options = ProvisioningTestHelpers.CreateOptions();
        var environment = new TestHostEnvironment
        {
            ApplicationName = "TestApp",
            EnvironmentName = environmentName ?? "Test"
        };
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();

        builder.Services.AddSingleton<IHostEnvironment>(environment);
        builder.Services.AddSingleton(armClientProvider);
        builder.Services.AddSingleton(userPrincipalProvider);
        builder.Services.AddSingleton(tokenCredentialProvider);
        builder.Services.AddSingleton(logger);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IProvisioningContextProvider, PublishModeProvisioningContextProvider>();
        builder.Services.AddSingleton<IDeploymentStateManager, FileDeploymentStateManager>();

        if (bicepProvisioner is not null)
        {
            builder.Services.AddSingleton(bicepProvisioner);
        }

        builder.Services.AddSingleton<IProcessRunner>(new MockProcessRunner());
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();
    }

    private sealed class TestPublishingActivityReporter : IPipelineActivityReporter
    {
        public bool CompletePublishCalled { get; private set; }
        public string? CompletionMessage { get; private set; }
        public List<string> CreatedSteps { get; } = [];
        public List<(string StepTitle, string TaskStatusText)> CreatedTasks { get; } = [];
        public List<(string StepTitle, string CompletionText, CompletionState CompletionState)> CompletedSteps { get; } = [];
        public List<(string TaskStatusText, string? CompletionMessage, CompletionState CompletionState)> CompletedTasks { get; } = [];
        public List<(string TaskStatusText, string StatusText)> UpdatedTasks { get; } = [];

        public Task CompletePublishAsync(string? completionMessage = null, CompletionState? completionState = null, bool isDeploy = false, CancellationToken cancellationToken = default)
        {
            CompletePublishCalled = true;
            CompletionMessage = completionMessage;
            return Task.CompletedTask;
        }

        public Task<IReportingStep> CreateStepAsync(string title, CancellationToken cancellationToken = default)
        {
            CreatedSteps.Add(title);
            return Task.FromResult<IReportingStep>(new TestReportingStep(this, title));
        }

        private sealed class TestReportingStep : IReportingStep
        {
            private readonly TestPublishingActivityReporter _reporter;
            private readonly string _title;

            public TestReportingStep(TestPublishingActivityReporter reporter, string title)
            {
                _reporter = reporter;
                _title = title;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
            {
                _reporter.CompletedSteps.Add((_title, completionText, completionState));
                return Task.CompletedTask;
            }

            public Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
            {
                _reporter.CreatedTasks.Add((_title, statusText));
                return Task.FromResult<IReportingTask>(new TestReportingTask(_reporter, statusText));
            }
        }

        private sealed class TestReportingTask : IReportingTask
        {
            private readonly TestPublishingActivityReporter _reporter;
            private readonly string _initialStatusText;

            public TestReportingTask(TestPublishingActivityReporter reporter, string initialStatusText)
            {
                _reporter = reporter;
                _initialStatusText = initialStatusText;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
            {
                _reporter.CompletedTasks.Add((_initialStatusText, completionMessage, completionState));
                return Task.CompletedTask;
            }

            public Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
            {
                _reporter.UpdatedTasks.Add((_initialStatusText, statusText));
                return Task.CompletedTask;
            }
        }
    }
}
