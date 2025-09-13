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
using Aspire.Hosting.Testing;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Aspire.TestUtilities;
using Aspire.Hosting.ApplicationModel;

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

        // Verify specific tag call was made (local "api" to target in testregistry)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "api" && call.targetImageName.StartsWith("testregistry.azurecr.io/"));

        // Verify specific push call was made
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/"));
    }

    [Fact]
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

        // Verify specific tag call was made (local "api" to target in testregistry)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "api" && call.targetImageName.StartsWith("testregistry.azurecr.io/"));

        // Verify specific push call was made
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/"));
    }

    [Fact]
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

        // Verify tag calls were made for both registries
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "api-service" && call.targetImageName.StartsWith("aasregistry.azurecr.io/"));
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "python-app" && call.targetImageName.StartsWith("acaregistry.azurecr.io/"));

        // Verify push calls were made for both registries
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("aasregistry.azurecr.io/"));
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("acaregistry.azurecr.io/"));

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

        // Wait for the notification interaction first
        var notificationInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved parameters", notificationInteraction.Title);
        Assert.Equal("There are unresolved parameters that need to be set. Please provide values for them.", notificationInteraction.Message);

        // Complete the notification interaction to proceed to inputs dialog
        notificationInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        // Wait for the parameter inputs interaction
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the parameter input (should include save to secrets option)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("test-param", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.Equal("Enter value for test-param", input.Placeholder);
            },
            input =>
            {
                Assert.Equal("Save to user secrets", input.Label);
                Assert.Equal(InputType.Boolean, input.InputType);
                Assert.False(input.Required);
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

        // Wait for the notification interaction first
        var notificationInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved parameters", notificationInteraction.Title);

        // Complete the notification interaction to proceed to inputs dialog
        notificationInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        // Wait for the parameter inputs interaction
        var parameterInputs = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", parameterInputs.Title);

        // Verify the custom input generator is respected (should include save to secrets option)
        Assert.Collection(parameterInputs.Inputs,
            input =>
            {
                Assert.Equal("custom-param", input.Name);
                Assert.Equal("Custom Port Number", input.Label);
                Assert.Equal("Enter a custom port number for the service", input.Description);
                Assert.Equal(InputType.Number, input.InputType);
                Assert.Equal("8080", input.Placeholder);
                Assert.False(input.EnableDescriptionMarkdown);
            },
            input =>
            {
                Assert.Equal("Save to user secrets", input.Label);
                Assert.Equal(InputType.Boolean, input.InputType);
                Assert.False(input.Required);
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
        var funcAppDeployment = Assert.IsType<AzureProvisioningResource>(funcApp.Resource.GetDeploymentTargetAnnotation()?.DeploymentTarget);
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

        // Verify specific tag call was made (local "funcapp" to target in testregistry)
        Assert.Contains(mockImageBuilder.TagImageCalls, call =>
            call.localImageName == "funcapp" && call.targetImageName.StartsWith("testregistry.azurecr.io/"));

        // Verify specific push call was made
        Assert.Contains(mockImageBuilder.PushImageCalls, imageName =>
            imageName.StartsWith("testregistry.azurecr.io/"));
    }

    private static void ConfigureTestServices(IDistributedApplicationTestingBuilder builder,
        IInteractionService? interactionService = null,
        IBicepProvisioner? bicepProvisioner = null,
        IArmClientProvider? armClientProvider = null,
        MockProcessRunner? processRunner = null,
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
        builder.Services.AddSingleton<IProvisioningContextProvider, PublishModeProvisioningContextProvider>();
        builder.Services.AddSingleton<IUserSecretsManager, NoOpUserSecretsManager>();
        if (bicepProvisioner is not null)
        {
            builder.Services.AddSingleton(bicepProvisioner);
        }
        builder.Services.AddSingleton<IProcessRunner>(processRunner ?? new MockProcessRunner());
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();
    }

    private sealed class NoOpUserSecretsManager : IUserSecretsManager
    {
        public Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new JsonObject());

        public Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
}
