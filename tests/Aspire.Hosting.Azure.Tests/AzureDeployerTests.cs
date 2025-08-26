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

namespace Aspire.Hosting.Azure.Tests;

public class AzureDeployerTests(ITestOutputHelper output)
{
    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11105")]
    public void DeployAsync_EmitsPublishedResources()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-deployer-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName, isDeploy: true);
        // Configure Azure settings to avoid prompting during deployment for this test case
        ConfigureTestServices(builder, bicepProvisioner: new NoOpBicepProvisioner());

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Add a container that will use the container app environment
        builder.AddContainer("api", "my-api-image:latest")
            .WithHttpEndpoint();

        // Act
        using var app = builder.Build();
        app.Run();

        // Assert files exist but don't verify contents
        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.True(File.Exists(mainBicepPath));
        var envBicepPath = Path.Combine(tempDir.FullName, "env", "env.bicep");
        Assert.True(File.Exists(envBicepPath));

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

        // Assert - Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", messageBarInteraction.Title);
        Assert.Contains("Azure resources that require an Azure Subscription", messageBarInteraction.Message ?? "");

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true)); // Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", inputsInteraction.Title);
        Assert.True(inputsInteraction.Options!.EnableMessageMarkdown);

        // Verify the expected inputs for Azure provisioning
        Assert.Collection(inputsInteraction.Inputs,
            input =>
            {
                Assert.Equal("Location", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal("Subscription ID", input.Label);
                Assert.Equal(InputType.SecretText, input.InputType);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal("Resource group", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.False(input.Required);
            });

        // Complete the inputs interaction with valid values
        inputsInteraction.Inputs[0].Value = inputsInteraction.Inputs[0].Options!.First(kvp => kvp.Key == "westus").Value;
        inputsInteraction.Inputs[1].Value = "12345678-1234-1234-1234-123456789012";
        inputsInteraction.Inputs[2].Value = "test-rg";

        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

        // Wait for the run task to complete (or timeout)
        await runTask.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task DeployAsync_WithAzureStorageResourcesWorks()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var deploymentOutputs = new Dictionary<string, object>
        {
            ["env_AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["env_AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        };
        var armClientProvider = new TestArmClientProvider(deploymentOutputs);
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

        // Assert that provisioning context values are passed into parameters on resource
        Assert.Equal(azureEnv.Resource.Parameters["location"], "westus2");
        Assert.Equal(azureEnv.Resource.Parameters["resourceGroupName"], "test-rg");
        Assert.Equal(azureEnv.Resource.Parameters["principalId"], "11111111-2222-3333-4444-555555555555");

        // Assert that ACR login command was not executed given no compute resources
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");
    }

    [Fact]
    public async Task DeployAsync_WithContainer_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var deploymentOutputs = new Dictionary<string, object>
        {
            ["env_AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["env_AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        };
        var armClientProvider = new TestArmClientProvider(deploymentOutputs);
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

        // Assert - Verify ACR login command was called
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath.Contains("az") &&
                   cmd.Arguments == "acr login --name testregistry");

        // Assert - Verify Docker tag and push not called for existing container image
        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath == "docker" &&
                   cmd.Arguments != null &&
                   cmd.Arguments.StartsWith("tag api testregistry.azurecr.io/"));

        Assert.DoesNotContain(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath == "docker" &&
                   cmd.Arguments != null &&
                   cmd.Arguments.StartsWith("push testregistry.azurecr.io/"));
    }

    [Fact]
    public async Task DeployAsync_WithProjectResource_Works()
    {
        // Arrange
        var mockProcessRunner = new MockProcessRunner();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", isDeploy: true);
        var deploymentOutputs = new Dictionary<string, object>
        {
            ["env_AZURE_CONTAINER_REGISTRY_NAME"] = new { type = "String", value = "testregistry" },
            ["env_AZURE_CONTAINER_REGISTRY_ENDPOINT"] = new { type = "String", value = "testregistry.azurecr.io" },
            ["env_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/test-identity" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"] = new { type = "String", value = "test.westus.azurecontainerapps.io" },
            ["env_AZURE_CONTAINER_APPS_ENVIRONMENT_ID"] = new { type = "String", value = "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.App/managedEnvironments/testenv" }
        };
        var armClientProvider = new TestArmClientProvider(deploymentOutputs);
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

        // Assert - Verify Docker tag and push called for project resources
        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath == "docker" &&
                   cmd.Arguments != null &&
                   cmd.Arguments.StartsWith("tag api testregistry.azurecr.io/"));

        Assert.Contains(mockProcessRunner.ExecutedCommands,
            cmd => cmd.ExecutablePath == "docker" &&
                   cmd.Arguments != null &&
                   cmd.Arguments.StartsWith("push testregistry.azurecr.io/"));
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
        builder.Services.AddSingleton<IProvisioningContextProvider, DefaultProvisioningContextProvider>();
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
}
