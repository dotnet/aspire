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
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure.Tests;

public class AzureDeployerTests(ITestOutputHelper output)
{
    [Fact]
    public void DeployAsync_EmitsPublishedResources()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-deployer-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName, isDeploy: true);
        // Configure Azure settings to avoid prompting during deployment for this test case
        builder.Configuration["Azure:SubscriptionId"] = "12345678-1234-1234-1234-123456789012";
        builder.Configuration["Azure:ResourceGroup"] = "test-rg";
        builder.Configuration["Azure:Location"] = "westus2";

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
        ConfigureTestServices(builder, testInteractionService);

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

    private static void ConfigureTestServices(IDistributedApplicationTestingBuilder builder, IInteractionService interactionService)
    {
        var options = ProvisioningTestHelpers.CreateOptions(null, null, null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        builder.Services.AddSingleton(armClientProvider);
        builder.Services.AddSingleton(userPrincipalProvider);
        builder.Services.AddSingleton(tokenCredentialProvider);
        builder.Services.AddSingleton(environment);
        builder.Services.AddSingleton(logger);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(interactionService);
        builder.Services.AddSingleton<IProvisioningContextProvider, DefaultProvisioningContextProvider>();
        builder.Services.AddSingleton<IUserSecretsManager, NoOpUserSecretsManager>();
        builder.Services.AddSingleton<IBicepProvisioner, NoOpBicepProvisioner>();
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
}
