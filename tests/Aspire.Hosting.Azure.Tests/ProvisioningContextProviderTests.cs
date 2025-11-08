// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREINTERACTION001
#pragma warning disable ASPIREPIPELINES002
#pragma warning disable ASPIREPIPELINES001

using System.Reflection;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class ProvisioningContextProviderTests
{
    private readonly TestInteractionService _defaultInteractionService = new() { IsAvailable = false };

    [Fact]
    public async Task CreateProvisioningContextAsync_ReturnsValidContext()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Credential);
        Assert.NotNull(context.ArmClient);
        Assert.NotNull(context.Subscription);
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.Tenant);
        Assert.NotNull(context.Location.DisplayName);
        Assert.NotNull(context.Principal);
        Assert.Equal("westus2", context.Location.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_ThrowsWhenSubscriptionIdMissing()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(subscriptionId: null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(CancellationToken.None));
        Assert.Contains("Azure subscription id is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_ThrowsWhenLocationMissing()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(location: null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            new TestInteractionService() { IsAvailable = false },
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(CancellationToken.None));
        Assert.Contains("azure location/region is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_GeneratesResourceGroupNameWhenNotProvided()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions(resourceGroup: null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.ResourceGroup.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_UsesProvidedResourceGroupName()
    {
        // Arrange
        var resourceGroupName = "my-custom-rg";
        var options = ProvisioningTestHelpers.CreateOptions(resourceGroup: resourceGroupName);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.Equal(resourceGroupName, context.ResourceGroup.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_RetrievesUserPrincipal()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

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
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

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
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            testInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));
        // Act
        var createTask = provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert - Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", messageBarInteraction.Title);

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));// Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", inputsInteraction.Title);
        Assert.True(inputsInteraction.Options!.EnableMessageMarkdown);

        Assert.Collection(inputsInteraction.Inputs,
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.TenantName, input.Name);
                Assert.Equal("Tenant ID", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.SubscriptionIdName, input.Name);
                Assert.Equal("Subscription ID", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.ResourceGroupName, input.Name);
                Assert.Equal("Resource group", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.False(input.Required);
            },
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.LocationName, input.Name);
                Assert.Equal("Location", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            });

        inputsInteraction.Inputs[BaseProvisioningContextProvider.SubscriptionIdName].Value = "12345678-1234-1234-1234-123456789012";

        // Trigger dynamic update of resource groups based on subscription.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        // Set a custom resource group name (new resource group)
        inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].Value = "test-new-rg";

        // Trigger dynamic update of locations based on subscription and resource group.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Value = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Options!.First(kvp => kvp.Key == "westus").Value;
        inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].Value = "rg-myrg";

        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

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
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            testInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        var createTask = provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));// Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        inputsInteraction.Inputs[BaseProvisioningContextProvider.SubscriptionIdName].Value = "not a guid";

        // Trigger dynamic update of locations based on subscription.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Value = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Options!.First(kvp => kvp.Key == "westus").Value;
        inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].Value = "invalid group";

        var context = new InputsDialogValidationContext
        {
            CancellationToken = CancellationToken.None,
            Services = new ServiceCollection().BuildServiceProvider(),
            Inputs = inputsInteraction.Inputs
        };

        var inputOptions = Assert.IsType<InputsDialogInteractionOptions>(inputsInteraction.Options);
        Assert.NotNull(inputOptions.ValidationCallback);
        await inputOptions.ValidationCallback(context);

        Assert.True((bool)context.GetType().GetProperty("HasErrors", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(context, null)!);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_DoesNotPromptForTenantWhenSubscriptionIdProvided()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var subscriptionId = "12345678-1234-1234-1234-123456789012";
        var options = ProvisioningTestHelpers.CreateOptions(subscriptionId, null, null);
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new RunModeProvisioningContextProvider(
            testInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

        // Act
        var createTask = provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert - Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", messageBarInteraction.Title);

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true)); // Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Azure provisioning", inputsInteraction.Title);
        Assert.True(inputsInteraction.Options!.EnableMessageMarkdown);

        // Assert that only 3 inputs are present (no tenant input since subscription is provided)
        Assert.Collection(inputsInteraction.Inputs,
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.SubscriptionIdName, input.Name);
                Assert.Equal("Subscription ID", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.True(input.Disabled);
                Assert.True(input.Required);
            },
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.ResourceGroupName, input.Name);
                Assert.Equal("Resource group", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.False(input.Required);
            },
            input =>
            {
                Assert.Equal(BaseProvisioningContextProvider.LocationName, input.Name);
                Assert.Equal("Location", input.Label);
                Assert.Equal(InputType.Choice, input.InputType);
                Assert.True(input.Required);
            });

        // Trigger dynamic update of resource groups based on subscription.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        // Set a custom resource group name
        inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].Value = "test-new-rg";

        // Trigger dynamic update of locations based on subscription and resource group.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        // Trigger dynamic update of locations based on subscription.
        await inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].DynamicLoading!.LoadCallback(new LoadInputContext
        {
            AllInputs = inputsInteraction.Inputs,
            CancellationToken = CancellationToken.None,
            Input = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName],
            Services = new ServiceCollection().BuildServiceProvider()
        });

        inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Value = inputsInteraction.Inputs[BaseProvisioningContextProvider.LocationName].Options!.First(kvp => kvp.Key == "westus").Value;
        inputsInteraction.Inputs[BaseProvisioningContextProvider.ResourceGroupName].Value = "rg-myrg";

        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

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
    public async Task PublishMode_CreateProvisioningContextAsync_ReturnsValidContext()
    {
        // Arrange
        var options = ProvisioningTestHelpers.CreateOptions();
        var environment = ProvisioningTestHelpers.CreateEnvironment();
        var logger = ProvisioningTestHelpers.CreateLogger<PublishModeProvisioningContextProvider>();
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var deploymentStateManager = ProvisioningTestHelpers.CreateUserSecretsManager();

        var provider = new PublishModeProvisioningContextProvider(
            _defaultInteractionService,
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider,
            deploymentStateManager,
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
            new NullPublishingActivityReporter());

        // Act
        var context = await provider.CreateProvisioningContextAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.Credential);
        Assert.NotNull(context.ArmClient);
        Assert.NotNull(context.Subscription);
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.Tenant);
        Assert.NotNull(context.Location.DisplayName);
        Assert.NotNull(context.Principal);
        Assert.Equal("westus2", context.Location.Name);
    }

    [Fact]
    public async Task GetAvailableResourceGroupsAsync_ReturnsResourceGroups()
    {
        // Arrange
        var armClientProvider = ProvisioningTestHelpers.CreateArmClientProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.CreateTokenCredentialProvider();
        var credential = tokenCredentialProvider.TokenCredential;
        var armClient = armClientProvider.GetArmClient(credential);
        var subscriptionId = "12345678-1234-1234-1234-123456789012";

        // Act
        var resourceGroups = await armClient.GetAvailableResourceGroupsAsync(subscriptionId, CancellationToken.None);

        // Assert
        Assert.NotNull(resourceGroups);
        var resourceGroupList = resourceGroups.ToList();
        Assert.NotEmpty(resourceGroupList);
        Assert.Contains("rg-test-1", resourceGroupList);
        Assert.Contains("rg-test-2", resourceGroupList);
        Assert.Contains("rg-aspire-dev", resourceGroupList);
    }
}
