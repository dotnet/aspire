// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultProvisioningContextProviderTests
{
    [Fact]
    public async Task CreateProvisioningContextAsync_ReturnsValidContext()
    {
        // Arrange
        var options = CreateOptions();
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

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
        var options = CreateOptions(subscriptionId: null);
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(userSecrets));
        Assert.Contains("Azure subscription id is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_ThrowsWhenLocationMissing()
    {
        // Arrange
        var options = CreateOptions(location: null);
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(userSecrets));
        Assert.Contains("azure location/region is required", exception.Message);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_GeneratesResourceGroupNameWhenNotProvided()
    {
        // Arrange
        var options = CreateOptions(resourceGroup: null);
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.NotNull(context.ResourceGroup.Data.Name);
        
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
        var options = CreateOptions(resourceGroup: resourceGroupName);
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.ResourceGroup);
        Assert.Equal(resourceGroupName, context.ResourceGroup.Data.Name);
    }

    [Fact]
    public async Task CreateProvisioningContextAsync_RetrievesUserPrincipal()
    {
        // Arrange
        var options = CreateOptions();
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

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
        var options = CreateOptions();
        var environment = CreateEnvironment();
        var logger = CreateLogger();
        var armClientProvider = ProvisioningTestHelpers.Instance.CreateArmClientProvider();
        var userPrincipalProvider = ProvisioningTestHelpers.Instance.CreateUserPrincipalProvider();
        var tokenCredentialProvider = ProvisioningTestHelpers.Instance.CreateTokenCredentialProvider();
        var userSecrets = new JsonObject();

        var provider = new DefaultProvisioningContextProvider(
            options,
            environment,
            logger,
            armClientProvider,
            userPrincipalProvider,
            tokenCredentialProvider);

        // Act
        var context = await provider.CreateProvisioningContextAsync(userSecrets);

        // Assert
        Assert.NotNull(context.Tenant);
        Assert.Equal(Guid.Parse("87654321-4321-4321-4321-210987654321"), context.Tenant.Data.TenantId);
        Assert.Equal("testdomain.onmicrosoft.com", context.Tenant.Data.DefaultDomain);
    }

    private static IOptions<AzureProvisionerOptions> CreateOptions(
        string? subscriptionId = "12345678-1234-1234-1234-123456789012",
        string? location = "westus2",
        string? resourceGroup = "test-rg")
    {
        var options = new AzureProvisionerOptions
        {
            SubscriptionId = subscriptionId,
            Location = location,
            ResourceGroup = resourceGroup
        };
        return Options.Create(options);
    }

    private static IHostEnvironment CreateEnvironment()
    {
        var environment = new TestHostEnvironment
        {
            ApplicationName = "TestApp"
        };
        return environment;
    }

    private static ILogger<DefaultProvisioningContextProvider> CreateLogger()
    {
        return NullLogger<DefaultProvisioningContextProvider>.Instance;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}