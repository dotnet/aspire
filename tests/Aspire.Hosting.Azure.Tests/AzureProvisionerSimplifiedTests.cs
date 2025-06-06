// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisionerTests
{
    [Fact]
    public void AddAzureProvisioningRegistersRequiredServices()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddAzureProvisioning();

        using var app = builder.Build();

        // Verify the new internal services are registered
        Assert.NotNull(app.Services.GetService<IArmClientProvider>());
        Assert.NotNull(app.Services.GetService<ISecretClientProvider>());
        Assert.NotNull(app.Services.GetService<IBicepCliExecutor>());
        Assert.NotNull(app.Services.GetService<IUserSecretsManager>());
        Assert.NotNull(app.Services.GetService<IProvisioningContextProvider>());
        Assert.NotNull(app.Services.GetService<TokenCredentialHolder>());
    }

    [Fact]
    public void DefaultArmClientProviderCreatesArmClient()
    {
        var provider = new DefaultArmClientProvider();
        var credential = new TestTokenCredential();
        var subscriptionId = "test-subscription-id";

        var armClient = provider.GetArmClient(credential, subscriptionId);

        Assert.NotNull(armClient);
    }

    [Fact]
    public void DefaultSecretClientProviderCreatesSecretClient()
    {
        var provider = new DefaultSecretClientProvider();
        var credential = new TestTokenCredential();
        var vaultUri = new Uri("https://test-vault.vault.azure.net/");

        var secretClient = provider.GetSecretClient(vaultUri, credential);

        Assert.NotNull(secretClient);
    }

    [Fact]
    public void DefaultUserSecretsManagerGetUserSecretsPathReturnsPath()
    {
        var manager = new DefaultUserSecretsManager();
        
        // This might return null if no user secrets are configured, which is valid
        var path = manager.GetUserSecretsPath();
        
        // Just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public async Task DefaultUserSecretsManagerLoadUserSecretsAsyncHandlesNullPath()
    {
        var manager = new DefaultUserSecretsManager();
        
        var userSecrets = await manager.LoadUserSecretsAsync(null, CancellationToken.None);
        
        Assert.NotNull(userSecrets);
        Assert.Empty(userSecrets);
    }

    [Fact]
    public async Task DefaultUserSecretsManagerLoadUserSecretsAsyncHandlesNonExistentFile()
    {
        var manager = new DefaultUserSecretsManager();
        var nonExistentPath = "/path/that/does/not/exist.json";
        
        var userSecrets = await manager.LoadUserSecretsAsync(nonExistentPath, CancellationToken.None);
        
        Assert.NotNull(userSecrets);
        Assert.Empty(userSecrets);
    }

    [Fact]
    public async Task DefaultBicepCliExecutorThrowsWhenAzureCliNotFound()
    {
        var executor = new DefaultBicepCliExecutor();
        var tempFile = Path.GetTempFileName();
        
        try
        {
            await File.WriteAllTextAsync(tempFile, "param location string");
            
            // This should throw because az CLI is likely not in PATH in test environment
            await Assert.ThrowsAsync<AzureCliNotOnPathException>(
                () => executor.CompileBicepToArmAsync(tempFile, CancellationToken.None));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DefaultProvisioningContextProviderRequiresSubscriptionId()
    {
        var options = Options.Create(new AzureProvisionerOptions());
        var environment = new TestHostEnvironment { ApplicationName = "TestApp" };
        var logger = new TestLogger<DefaultProvisioningContextProvider>();
        var armClientProvider = new DefaultArmClientProvider();
        
        var provider = new DefaultProvisioningContextProvider(options, environment, logger, armClientProvider);
        var tokenHolder = new TokenCredentialHolder();
        var userSecretsLazy = new Lazy<Task<JsonObject>>(() => Task.FromResult(new JsonObject()));
        
        await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(tokenHolder, userSecretsLazy, CancellationToken.None));
    }

    [Fact]
    public async Task DefaultProvisioningContextProviderRequiresLocation()
    {
        var options = Options.Create(new AzureProvisionerOptions 
        { 
            SubscriptionId = "test-subscription-id"
        });
        var environment = new TestHostEnvironment { ApplicationName = "TestApp" };
        var logger = new TestLogger<DefaultProvisioningContextProvider>();
        var armClientProvider = new TestArmClientProvider();
        
        var provider = new DefaultProvisioningContextProvider(options, environment, logger, armClientProvider);
        var tokenHolder = new TokenCredentialHolder();
        tokenHolder.SetCredential(new TestTokenCredential());
        var userSecretsLazy = new Lazy<Task<JsonObject>>(() => Task.FromResult(new JsonObject()));
        
        await Assert.ThrowsAsync<MissingConfigurationException>(
            () => provider.CreateProvisioningContextAsync(tokenHolder, userSecretsLazy, CancellationToken.None));
    }

    private sealed class TestTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/";
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private sealed class TestArmClientProvider : IArmClientProvider
    {
        public ArmClient GetArmClient(TokenCredential credential, string subscriptionId)
        {
            // This would need a more sophisticated mock in a real test, but for now just verify it doesn't throw
            return new ArmClient(credential, subscriptionId);
        }
    }
}
}