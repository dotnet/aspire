// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultProvisioningContextProviderTests
{
    private readonly TestInteractionService _defaultInteractionService = new() { IsAvailable = false };

    [Fact]
    public async Task CreateProvisioningContextAsync_ReturnsValidContext()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Credential);
        Assert.NotNull(context.ArmClient);
        Assert.NotNull(context.Subscription);
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.Tenant);
        Assert.NotNull(context.Location.DisplayName);
        Assert.NotNull(context.Principal);
        Assert.NotNull(context.UserSecrets);
        Assert.Equal("westus2", context.Location.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_ThrowsWhenSubscriptionIdMissing()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(subscriptionId: null);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(userSecrets));
        Assert.Contains("Azure subscription id is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_ThrowsWhenLocationMissing()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(location: null);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            new TestInteractionService() { IsAvailable = false },
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(userSecrets));
        Assert.Contains("azure location/region is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_GeneratesResourceGroupNameWhenNotProvided()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(resourceGroup: null);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.ResourceGroup.Name);

        // Verify that the resource group name was saved to user secrets
        var azureSettings = userSecrets["Azure"] as JsonObject;
        Assert.NotNull(azureSettings);
        Assert.NotNull(azureSettings["ResourceGroup"]);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_UsesProvidedResourceGroupName()
    {
        // Arrange
        var resourceGroupName = "my-custom-rg";
        var options = ProvisioningTestHelpers.CreateOptions(resourceGroup: resourceGroupName);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.Equal(resourceGroupName, context.ResourceGroup.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_RetrievesUserPrincipal()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.Principal);
        Assert.Equal("test@example.com", context.Principal.Name);
        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), context.Principal.Id);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_SetsCorrectTenant()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.Tenant);
        Assert.Equal(Guid.Parse("87654321-4321-4321-4321-210987654321"), context.Tenant.TenantId);
        Assert.Equal("testdomain.onmicrosoft.com", context.Tenant.DefaultDomain);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_PromptsIfNoOptions()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var options = ProvisioningTestHelpers.CreateOptions(null, null, null);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            testInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);
        // Act
        var createTask = provider.CreateProvisioningContextAsync(userSecrets);

        // Assert - Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", messageBarInteraction.Title);

        // Complete the message bar interaction to proceed to subscription dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));// Data = true (user clicked Enter Values)

        // Wait for the subscription selection interaction
        var subscriptionInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure subscription", subscriptionInteraction.Title);
        Assert.True(subscriptionInteraction.Options!.EnableMessageMarkdown);

        Assert.Collection(subscriptionInteraction.Inputs,
            input =>
            {
                Assert.Equal("Subscription", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            });

        // Select the first subscription from the test data
        subscriptionInteraction.Inputs[0].Value = subscriptionInteraction.Inputs[0].Options![0].Key;

        subscriptionInteraction.CompletionTcs.SetResult(InteractionResult.Ok(subscriptionInteraction.Inputs));

        // Wait for the location and resource group interaction
        var locationInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning details", locationInteraction.Title);
        Assert.True(locationInteraction.Options!.EnableMessageMarkdown);

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

        locationInteraction.Inputs[0].Value = locationInteraction.Inputs[0].Options!.First(kvp => kvp.Key == "westus").Value;
        locationInteraction.Inputs[1].Value = "rg-myrg";

        locationInteraction.CompletionTcs.SetResult(InteractionResult.Ok(locationInteraction.Inputs));

        // Wait for the create task to complete
        var context = await createTask;

        // Assert
        Assert.NotNull(context.Tenant);
        Assert.Equal(Guid.Parse("87654321-4321-4321-4321-210987654321"), context.Tenant.TenantId);
        Assert.Equal("testdomain.onmicrosoft.com", context.Tenant.DefaultDomain);
        Assert.Equal("/subscriptions/12345678-1234-1234-1234-123456789012", context.Subscription.Id.ToString());
        Assert.Equal("westus", context.Location.Name);
        Assert.Equal("rg-myrg", context.ResourceGroup.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_Prompt_ValidatesSubAndResourceGroup()
    {
        var testInteractionService = new TestInteractionService();
        var options = ProvisioningTestHelpers.CreateOptions(null, null, null);
        var publishingOptions = ProvisioningTestHelpers.CreatePublishingOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            testInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            publishingOptions);

        var createTask = provider.CreateProvisioningContextAsync(userSecrets);

        // Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        // Complete the message bar interaction to proceed to subscription dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));// Data = true (user clicked Enter Values)

        // Wait for the subscription selection interaction
        var subscriptionInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        // Select the first subscription from the test data
        subscriptionInteraction.Inputs[0].Value = subscriptionInteraction.Inputs[0].Options![0].Key;
        subscriptionInteraction.CompletionTcs.SetResult(InteractionResult.Ok(subscriptionInteraction.Inputs));

        // Wait for the location and resource group interaction
        var locationInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        locationInteraction.Inputs[0].Value = locationInteraction.Inputs[0].Options!.First(kvp => kvp.Key == "westus").Value;
        locationInteraction.Inputs[1].Value = "invalid group";

        var context = new InputsDialogValidationContext
        {
            CancellationToken = CancellationToken.None,
            ServiceProvider = new ServiceCollection().BuildServiceProvider(),
            Inputs = locationInteraction.Inputs
        };

        var inputOptions = Assert.IsType<InputsDialogInteractionOptions>(locationInteraction.Options);
        Assert.NotNull(inputOptions.ValidationCallback);
        await inputOptions.ValidationCallback(context);

        Assert.True((bool)context.GetType().GetProperty("HasErrors", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(context, null)!);
    }

}
