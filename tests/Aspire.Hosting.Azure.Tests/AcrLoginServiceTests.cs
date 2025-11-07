// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECONTAINERRUNTIME001
#pragma warning disable ASPIREPIPELINES002
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Publishing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AcrLoginServiceTests
{
    [Fact]
    public async Task LoginAsync_CachesTokenOnFirstLogin()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // Act
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - Token should be cached in a section named after the registry
        var section = await stateManager.AcquireSectionAsync("AcrTokens:myregistry_azurecr_io", CancellationToken.None);
        Assert.NotNull(section);
        Assert.True(section.Data.ContainsKey("tenant-id"));
        
        var tokenNode = section.Data["tenant-id"];
        Assert.NotNull(tokenNode);
        
        var refreshToken = tokenNode["refresh_token"]?.GetValue<string>();
        Assert.Equal("fake-refresh-token", refreshToken);
        
        var expiresAtUtc = tokenNode["expires_at_utc"]?.GetValue<DateTime>();
        Assert.NotNull(expiresAtUtc);
        Assert.True(expiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_UsesCachedTokenWhenValid()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // First login to cache the token
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);
        var firstLoginCount = httpClientFactory.LoginCallCount;
        var firstContainerLoginCount = containerRuntime.LoginToRegistryCalls.Count;

        // Act - Second login should use cached token
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - HTTP client should not be called again
        Assert.Equal(firstLoginCount, httpClientFactory.LoginCallCount);
        // Container runtime should still be called to perform the actual login
        Assert.Equal(firstContainerLoginCount + 1, containerRuntime.LoginToRegistryCalls.Count);
    }

    [Fact(Skip = "Test requires 6 minute delay - run manually if needed")]
    public async Task LoginAsync_RefreshesTokenWhenExpired()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 1); // Token expires in 1 second
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // First login to cache the token
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);
        var firstLoginCount = httpClientFactory.LoginCallCount;

        // Wait for token to expire (plus safety margin)
        await Task.Delay(TimeSpan.FromMinutes(6)); // Safety margin is 5 minutes

        // Act - Second login should get a fresh token
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - HTTP client should be called again
        Assert.Equal(firstLoginCount + 1, httpClientFactory.LoginCallCount);
    }

    [Fact]
    public async Task LoginAsync_CachesDifferentTokensForDifferentRegistries()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // Act - Login to two different registries
        await service.LoginAsync("registry1.azurecr.io", "tenant-id", credential, CancellationToken.None);
        await service.LoginAsync("registry2.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - Both tokens should be cached in separate sections
        var section1 = await stateManager.AcquireSectionAsync("AcrTokens:registry1_azurecr_io", CancellationToken.None);
        Assert.True(section1.Data.ContainsKey("tenant-id"));
        
        var section2 = await stateManager.AcquireSectionAsync("AcrTokens:registry2_azurecr_io", CancellationToken.None);
        Assert.True(section2.Data.ContainsKey("tenant-id"));
    }

    [Fact]
    public async Task LoginAsync_CachesDifferentTokensForDifferentTenants()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // Act - Login to same registry with different tenants
        await service.LoginAsync("myregistry.azurecr.io", "tenant-1", credential, CancellationToken.None);
        await service.LoginAsync("myregistry.azurecr.io", "tenant-2", credential, CancellationToken.None);

        // Assert - Both tokens should be cached in the same section with different keys
        var section = await stateManager.AcquireSectionAsync("AcrTokens:myregistry_azurecr_io", CancellationToken.None);
        Assert.True(section.Data.ContainsKey("tenant-1"));
        Assert.True(section.Data.ContainsKey("tenant-2"));
    }

    [Fact]
    public async Task LoginAsync_RetriesWithFreshTokenOn401()
    {
        // Arrange
        var containerRuntime = new UnauthorizedOnFirstLoginContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // First login to cache a token
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);
        var firstLoginCount = httpClientFactory.LoginCallCount;

        // Reset the runtime to fail on the next cached token attempt
        containerRuntime.FailNextLogin = true;

        // Act - Second login should retry with fresh token when cached token gets 401
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - HTTP client should be called again to get fresh token
        Assert.Equal(firstLoginCount + 1, httpClientFactory.LoginCallCount);
        // Container runtime should have been called three times: first login, failed cached attempt, successful retry
        Assert.Equal(3, containerRuntime.LoginCallCount);
    }

    [Fact]
    public async Task LoginAsync_DefaultsToThreeHoursWhenExpiresInNotProvided()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: null); // No expires_in
        var stateManager = ProvisioningTestHelpers.CreateUserSecretsManager();
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // Act
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - Token should be cached with 3-hour expiration (10800 seconds)
        var section = await stateManager.AcquireSectionAsync("AcrTokens:myregistry_azurecr_io", CancellationToken.None);
        var tokenNode = section.Data["tenant-id"];
        var expiresAtUtc = tokenNode!["expires_at_utc"]?.GetValue<DateTime>();
        
        Assert.NotNull(expiresAtUtc);
        var expectedExpiration = DateTime.UtcNow.AddSeconds(10800);
        // Allow 10 second tolerance for test execution time
        Assert.True(Math.Abs((expiresAtUtc.Value - expectedExpiration).TotalSeconds) < 10);
    }

    [Fact]
    public async Task LoginAsync_ContinuesWhenCachingFails()
    {
        // Arrange
        var containerRuntime = new FakeContainerRuntime();
        var httpClientFactory = new FakeHttpClientFactory(expiresIn: 3600);
        var stateManager = new ThrowingDeploymentStateManager(); // Throws on save
        var logger = NullLogger<AcrLoginService>.Instance;
        var service = new AcrLoginService(httpClientFactory, containerRuntime, stateManager, logger);
        var credential = new TestTokenCredential();

        // Act - Should not throw even though caching fails
        await service.LoginAsync("myregistry.azurecr.io", "tenant-id", credential, CancellationToken.None);

        // Assert - Container runtime should still be called
        Assert.Single(containerRuntime.LoginToRegistryCalls);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly int? _expiresIn;

        public int LoginCallCount { get; private set; }

        public FakeHttpClientFactory(int? expiresIn)
        {
            _expiresIn = expiresIn;
        }

        public HttpClient CreateClient(string name)
        {
            LoginCallCount++;
            var handler = new FakeHttpMessageHandler(_expiresIn);
            return new HttpClient(handler);
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly int? _expiresIn;

            public FakeHttpMessageHandler(int? expiresIn)
            {
                _expiresIn = expiresIn;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var json = _expiresIn.HasValue
                    ? $$"""{"refresh_token": "fake-refresh-token", "expires_in": {{_expiresIn}}}"""
                    : """{"refresh_token": "fake-refresh-token"}""";

                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }

    private sealed class UnauthorizedOnFirstLoginContainerRuntime : IContainerRuntime
    {
        public string Name => "fake-runtime";
        public int LoginCallCount { get; private set; }
        public bool FailNextLogin { get; set; }

        public Task LoginToRegistryAsync(string registryServer, string username, string password, CancellationToken cancellationToken = default)
        {
            LoginCallCount++;
            
            if (FailNextLogin)
            {
                FailNextLogin = false; // Only fail once
                throw new HttpRequestException("Login failed", null, System.Net.HttpStatusCode.Unauthorized);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> CheckIfRunningAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task BuildImageAsync(string contextPath, string dockerfilePath, string imageName, ContainerBuildOptions? options, Dictionary<string, string?> buildArguments, Dictionary<string, string?> buildSecrets, string? stage, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task TagImageAsync(string localImageName, string targetImageName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveImageAsync(string imageName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task PushImageAsync(string imageName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public static Task<bool> InspectImageAsync(string _, CancellationToken __ = default) => Task.FromResult(true);
        public static Task PullImageAsync(string _, CancellationToken __ = default) => Task.CompletedTask;
    }

    private sealed class ThrowingDeploymentStateManager : IDeploymentStateManager
    {
        public string? StateFilePath => null;

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
        {
            // Return a valid section for reading
            return Task.FromResult(new DeploymentStateSection(sectionName, new System.Text.Json.Nodes.JsonObject(), 0));
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default)
        {
            // Throw when saving to simulate failure
            throw new InvalidOperationException("Simulated save failure");
        }
    }
}
