// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Secrets;

namespace Aspire.Hosting.Azure.Tests;

/// <summary>
/// Test helpers for creating testable provisioning services.
/// </summary>
internal static class ProvisioningTestHelpers
{
    /// <summary>
    /// Creates a test-friendly ProvisioningContext.
    /// </summary>
    public static ProvisioningContext CreateTestProvisioningContext(
        TokenCredential? credential = null,
        IArmClient? armClient = null,
        ISubscriptionResource? subscription = null,
        IResourceGroupResource? resourceGroup = null,
        ITenantResource? tenant = null,
        IAzureLocation? location = null,
        UserPrincipal? principal = null,
        JsonObject? userSecrets = null)
    {
        return new ProvisioningContext(
            credential ?? new TestTokenCredential(),
            armClient ?? new TestArmClient(),
            subscription ?? new TestSubscriptionResource(),
            resourceGroup ?? new TestResourceGroupResource(),
            tenant ?? new TestTenantResource(),
            location ?? new TestAzureLocation(),
            principal ?? new UserPrincipal(Guid.NewGuid(), "test@example.com"),
            userSecrets ?? new JsonObject());
    }
}

/// <summary>
/// Test implementation of <see cref="TokenCredential"/>.
/// </summary>
internal sealed class TestTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = CreateTestJwtToken();
        return new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = CreateTestJwtToken();
        return ValueTask.FromResult(new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1)));
    }
    
    private static string CreateTestJwtToken()
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" });
        var headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        
        var payload = new
        {
            oid = "11111111-2222-3333-4444-555555555555",
            upn = "test@example.com",
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var signatureBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"{headerBase64}.{payloadBase64}.{signatureBase64}";
    }
}

/// <summary>
/// Test implementation of <see cref="IArmClient"/>.
/// </summary>
internal sealed class TestArmClient : IArmClient
{
    public Task<ISubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ISubscriptionResource>(new TestSubscriptionResource());
    }

    public async IAsyncEnumerable<ITenantResource> GetTenantsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new TestTenantResource();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation of <see cref="ISubscriptionResource"/>.
/// </summary>
internal sealed class TestSubscriptionResource : ISubscriptionResource
{
    public ResourceIdentifier Id { get; } = new ResourceIdentifier("/subscriptions/12345678-1234-1234-1234-123456789012");
    public ISubscriptionData Data { get; } = new TestSubscriptionData();

    public Task<IResourceGroupResource> GetResourceGroupAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IResourceGroupResource>(new TestResourceGroupResource(resourceGroupName));
    }

    public IResourceGroupCollection GetResourceGroups()
    {
        return new TestResourceGroupCollection();
    }
}

/// <summary>
/// Test implementation of <see cref="ISubscriptionData"/>.
/// </summary>
internal sealed class TestSubscriptionData : ISubscriptionData
{
    public ResourceIdentifier Id { get; } = new ResourceIdentifier("/subscriptions/12345678-1234-1234-1234-123456789012");
    public string? DisplayName { get; } = "Test Subscription";
    public Guid? TenantId { get; } = Guid.Parse("87654321-4321-4321-4321-210987654321");
}

/// <summary>
/// Test implementation of <see cref="IResourceGroupCollection"/>.
/// </summary>
internal sealed class TestResourceGroupCollection : IResourceGroupCollection
{
    public Task<Response<IResourceGroupResource>> GetAsync(string resourceGroupName, CancellationToken cancellationToken = default)
    {
        var resourceGroup = new TestResourceGroupResource(resourceGroupName);
        return Task.FromResult(Response.FromValue<IResourceGroupResource>(resourceGroup, new MockResponse(200)));
    }

    public Task<ArmOperation<IResourceGroupResource>> CreateOrUpdateAsync(WaitUntil waitUntil, string resourceGroupName, ResourceGroupData data, CancellationToken cancellationToken = default)
    {
        var resourceGroup = new TestResourceGroupResource(resourceGroupName);
        var operation = new TestArmOperation<IResourceGroupResource>(resourceGroup);
        return Task.FromResult<ArmOperation<IResourceGroupResource>>(operation);
    }
}

/// <summary>
/// Test implementation of <see cref="IResourceGroupResource"/>.
/// </summary>
internal sealed class TestResourceGroupResource : IResourceGroupResource
{
    private readonly string _name;

    public TestResourceGroupResource(string name = "test-rg")
    {
        _name = name;
    }

    public ResourceIdentifier Id { get; } = new ResourceIdentifier("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/test-rg");
    public IResourceGroupData Data => new TestResourceGroupData(_name);

    public IArmDeploymentCollection GetArmDeployments()
    {
        return new TestArmDeploymentCollection();
    }
}

/// <summary>
/// Test implementation of <see cref="IResourceGroupData"/>.
/// </summary>
internal sealed class TestResourceGroupData(string name) : IResourceGroupData
{
    public string Name { get; } = name;
}

/// <summary>
/// Test implementation of <see cref="IArmDeploymentCollection"/>.
/// </summary>
internal sealed class TestArmDeploymentCollection : IArmDeploymentCollection
{
    public Task<ArmOperation<ArmDeploymentResource>> CreateOrUpdateAsync(
        WaitUntil waitUntil, 
        string deploymentName, 
        ArmDeploymentContent content, 
        CancellationToken cancellationToken = default)
    {
        var deployment = new TestArmDeploymentResource(deploymentName);
        var operation = new TestArmOperation<ArmDeploymentResource>(deployment);
        return Task.FromResult<ArmOperation<ArmDeploymentResource>>(operation);
    }
}

/// <summary>
/// Test implementation of <see cref="ITenantResource"/>.
/// </summary>
internal sealed class TestTenantResource : ITenantResource
{
    public ITenantData Data { get; } = new TestTenantData();
}

/// <summary>
/// Test implementation of <see cref="ITenantData"/>.
/// </summary>
internal sealed class TestTenantData : ITenantData
{
    public Guid? TenantId { get; } = Guid.Parse("87654321-4321-4321-4321-210987654321");
    public string? DefaultDomain { get; } = "testdomain.onmicrosoft.com";
}

/// <summary>
/// Test implementation of <see cref="IAzureLocation"/>.
/// </summary>
internal sealed class TestAzureLocation : IAzureLocation
{
    public string Name { get; } = "westus2";

    public override string ToString() => Name;
}

/// <summary>
/// Test implementation of ArmOperation for testing.
/// </summary>
internal sealed class TestArmOperation<T>(T value) : ArmOperation<T>
{
    public override string Id { get; } = Guid.NewGuid().ToString();
    public override T Value { get; } = value;
    public override bool HasCompleted { get; } = true;
    public override bool HasValue { get; } = true;

    public override Response GetRawResponse() => new MockResponse(200);
    public override Response UpdateStatus(CancellationToken cancellationToken = default) => new MockResponse(200);
    public override ValueTask<Response> UpdateStatusAsync(CancellationToken cancellationToken = default) => new ValueTask<Response>(new MockResponse(200));
    public override ValueTask<Response<T>> WaitForCompletionAsync(CancellationToken cancellationToken = default) => new ValueTask<Response<T>>(Response.FromValue(Value, new MockResponse(200)));
    public override ValueTask<Response<T>> WaitForCompletionAsync(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => new ValueTask<Response<T>>(Response.FromValue(Value, new MockResponse(200)));
    public override Response<T> WaitForCompletion(CancellationToken cancellationToken = default) => Response.FromValue(Value, new MockResponse(200));
    public override Response<T> WaitForCompletion(TimeSpan pollingInterval, CancellationToken cancellationToken = default) => Response.FromValue(Value, new MockResponse(200));
}

/// <summary>
/// Test implementation of ArmDeploymentResource for testing.
/// </summary>
internal sealed class TestArmDeploymentResource(string name) : ArmDeploymentResource
{
    public override ResourceIdentifier Id { get; } = new ResourceIdentifier($"/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/test-rg/providers/Microsoft.Resources/deployments/{name}");
    public override ArmDeploymentData Data => throw new NotImplementedException("Test implementation doesn't provide data");
    public override bool HasData => false;
}

/// <summary>
/// Mock Response implementation for testing.
/// </summary>
internal sealed class MockResponse(int status) : Response
{
    public override int Status { get; } = status;
    public override string ReasonPhrase { get; } = "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();

    protected override bool ContainsHeader(string name) => false;
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => Enumerable.Empty<HttpHeader>();
    protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
    {
        value = default;
        return false;
    }
    protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
    {
        values = default;
        return false;
    }
    public override void Dispose() { }
}

/// <summary>
/// Test implementations for the provisioning services interfaces.
/// </summary>
internal static class TestProvisioningServices
{
    public static IArmClientProvider CreateArmClientProvider() => new TestArmClientProvider();
    public static ITokenCredentialProvider CreateTokenCredentialProvider() => new TestTokenCredentialProvider();
    public static ISecretClientProvider CreateSecretClientProvider() => new TestSecretClientProvider(CreateTokenCredentialProvider());
    public static IBicepCompiler CreateBicepCompiler() => new TestBicepCompiler();
    public static IUserSecretsManager CreateUserSecretsManager() => new TestUserSecretsManager();
    public static IUserPrincipalProvider CreateUserPrincipalProvider() => new TestUserPrincipalProvider();
    public static TokenCredential CreateTokenCredential() => new TestTokenCredential();
}

internal sealed class TestArmClientProvider : IArmClientProvider
{
    public IArmClient GetArmClient(TokenCredential credential, string subscriptionId)
    {
        return new TestArmClient();
    }
}

internal sealed class TestSecretClientProvider(ITokenCredentialProvider tokenCredentialProvider) : ISecretClientProvider
{
    public SecretClient GetSecretClient(Uri vaultUri)
    {
        var credential = tokenCredentialProvider.TokenCredential;
        return new SecretClient(vaultUri, credential);
    }
}

internal sealed class TestBicepCompiler : IBicepCompiler
{
    public Task<string> CompileBicepToArmAsync(string bicepFilePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(@"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {},
  ""resources"": []
}");
    }
}

internal sealed class TestUserSecretsManager : IUserSecretsManager
{
    private JsonObject _userSecrets = new JsonObject();

    public Task<JsonObject> LoadUserSecretsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_userSecrets);
    }

    public Task SaveUserSecretsAsync(JsonObject userSecrets, CancellationToken cancellationToken = default)
    {
        _userSecrets = userSecrets;
        return Task.CompletedTask;
    }
}

internal sealed class TestUserPrincipalProvider : IUserPrincipalProvider
{
    public Task<UserPrincipal> GetUserPrincipalAsync(CancellationToken cancellationToken = default)
    {
        var principal = new UserPrincipal(Guid.Parse("11111111-2222-3333-4444-555555555555"), "test@example.com");
        return Task.FromResult(principal);
    }
}

internal sealed class TestTokenCredentialProvider : ITokenCredentialProvider
{
    public TokenCredential TokenCredential => new TestTokenCredential();
}