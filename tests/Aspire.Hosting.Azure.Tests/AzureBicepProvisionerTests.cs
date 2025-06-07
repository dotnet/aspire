// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Utils;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Tests;

public class AzureBicepProvisionerTests
{
    [Theory]
    [InlineData("1alpha")]
    [InlineData("-alpha")]
    [InlineData("")]
    [InlineData(" alpha")]
    [InlineData("alpha 123")]
    public void WithParameterDoesNotAllowParameterNamesWhichAreInvalidBicepIdentifiers(string bicepParameterName)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var builder = TestDistributedApplicationBuilder.Create();
            builder.AddAzureInfrastructure("infrastructure", _ => { })
                   .WithParameter(bicepParameterName);
        });
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("a1pha")]
    [InlineData("_alpha")]
    [InlineData("__alpha")]
    [InlineData("alpha1_")]
    [InlineData("Alpha1_A")]
    public void WithParameterAllowsParameterNamesWhichAreValidBicepIdentifiers(string bicepParameterName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddAzureInfrastructure("infrastructure", _ => { })
                .WithParameter(bicepParameterName);
    }

    [Fact]
    public async Task NestedChildResourcesShouldGetUpdated()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        using var builder = TestDistributedApplicationBuilder.Create();

        var cosmos = builder.AddAzureCosmosDB("cosmosdb");
        var db = cosmos.AddCosmosDatabase("db");
        var entries = db.AddContainer("entries", "/id");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await foreach (var resourceEvent in rns.WatchAsync(cts.Token).WithCancellation(cts.Token))
        {
            if (resourceEvent.Resource == entries.Resource)
            {
                var parentProperty = resourceEvent.Snapshot.Properties.FirstOrDefault(x => x.Name == KnownProperties.Resource.ParentName)?.Value?.ToString();
                Assert.Equal("db", parentProperty);
                return;
            }
        }

        Assert.Fail();
    }

    [Fact]
    public void ShouldProvision_ReturnsFalse_WhenResourceIsContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;
        
        // Make the resource a container by adding the container annotation
        bicep.Annotations.Add(new ContainerImageAnnotation { Image = "test-image" });

        var result = BicepProvisioner.ShouldProvision(bicep);

        Assert.False(result);
    }

    [Fact]
    public void ShouldProvision_ReturnsTrue_WhenResourceIsNotContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var bicep = builder.AddBicepTemplateString("test", "param name string").Resource;

        var result = BicepProvisioner.ShouldProvision(bicep);

        Assert.True(result);
    }

    [Fact]
    public void BicepProvisioner_CanBeInstantiated()
    {
        // Test that BicepProvisioner can be instantiated with required dependencies
        
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var services = builder.Services.BuildServiceProvider();
        
        var bicepExecutor = new TestBicepCliExecutor();
        var secretClientProvider = new TestSecretClientProvider();
        var tokenCredentialHolder = new TestTokenCredentialHolder();
        
        // Act
        var provisioner = new BicepProvisioner(
            services.GetRequiredService<ResourceNotificationService>(),
            services.GetRequiredService<ResourceLoggerService>(),
            tokenCredentialHolder,
            bicepExecutor,
            secretClientProvider);
        
        // Assert
        Assert.NotNull(provisioner);
    }

    [Fact]
    public async Task BicepCliExecutor_CompilesBicepToArm()
    {
        // Test the mock bicep executor behavior
        
        // Arrange
        var bicepExecutor = new TestBicepCliExecutor();
        
        // Act
        var result = await bicepExecutor.CompileBicepToArmAsync("test.bicep", CancellationToken.None);
        
        // Assert
        Assert.True(bicepExecutor.CompileBicepToArmAsyncCalled);
        Assert.Equal("test.bicep", bicepExecutor.LastCompiledPath);
        Assert.NotNull(result);
        Assert.Contains("$schema", result);
    }

    [Fact]
    public void SecretClientProvider_CreatesSecretClient()
    {
        // Test the mock secret client provider behavior
        
        // Arrange
        var secretClientProvider = new TestSecretClientProvider();
        var vaultUri = new Uri("https://test.vault.azure.net/");
        var credential = new TestTokenCredential();
        
        // Act
        var client = secretClientProvider.GetSecretClient(vaultUri, credential);
        
        // Assert
        Assert.True(secretClientProvider.GetSecretClientCalled);
        // Client will be null in our mock, but the call was tracked
        Assert.Null(client);
    }

    [Fact]
    public void TestTokenCredential_ProvidesAccessToken()
    {
        // Test the mock token credential behavior
        
        // Arrange
        var credential = new TestTokenCredential();
        var requestContext = new TokenRequestContext(["https://management.azure.com/.default"]);
        
        // Act
        var token = credential.GetToken(requestContext, CancellationToken.None);
        
        // Assert
        Assert.Equal("mock-token", token.Token);
        Assert.True(token.ExpiresOn > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TestTokenCredential_ProvidesAccessTokenAsync()
    {
        // Test the mock token credential async behavior
        
        // Arrange
        var credential = new TestTokenCredential();
        var requestContext = new TokenRequestContext(["https://management.azure.com/.default"]);
        
        // Act
        var token = await credential.GetTokenAsync(requestContext, CancellationToken.None);
        
        // Assert
        Assert.Equal("mock-token", token.Token);
        Assert.True(token.ExpiresOn > DateTimeOffset.UtcNow);
    }

    private sealed class TestTokenCredentialHolder : TokenCredentialHolder
    {
        public TestTokenCredentialHolder() : base(
            new TestLogger(),
            new TestOptions())
        {
        }
        
        private sealed class TestLogger : ILogger<TokenCredentialHolder>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
        
        private sealed class TestOptions : IOptions<AzureProvisionerOptions>
        {
            public AzureProvisionerOptions Value { get; } = new() { CredentialSource = "AzureCli" };
        }
    }

    private sealed class TestTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => 
            new("mock-token", DateTimeOffset.UtcNow.AddHours(1));

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) => 
            ValueTask.FromResult(new AccessToken("mock-token", DateTimeOffset.UtcNow.AddHours(1)));
    }

    private sealed class TestBicepCliExecutor : IBicepCliExecutor
    {
        public bool CompileBicepToArmAsyncCalled { get; private set; }
        public string? LastCompiledPath { get; private set; }
        public string CompilationResult { get; set; } = """{"$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"}""";

        public Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default)
        {
            CompileBicepToArmAsyncCalled = true;
            LastCompiledPath = bicepFilePath;
            return Task.FromResult(CompilationResult);
        }
    }

    private sealed class TestSecretClientProvider : ISecretClientProvider
    {
        public bool GetSecretClientCalled { get; private set; }

        public SecretClient GetSecretClient(Uri vaultUri, TokenCredential credential)
        {
            GetSecretClientCalled = true;
            // Return null - this will fail in actual secret operations but allows testing the call
            return null!;
        }
    }
}
