// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Models;
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
        ArmClient? armClient = null,
        SubscriptionResource? subscription = null,
        ResourceGroupResource? resourceGroup = null,
        TenantResource? tenant = null,
        AzureLocation? location = null,
        UserPrincipal? principal = null,
        JsonObject? userSecrets = null)
    {
        return new ProvisioningContext(
            credential ?? new TestTokenCredential(),
            armClient ?? TestAzureResources.CreateTestArmClient(),
            subscription ?? TestAzureResources.CreateTestSubscriptionResource(),
            resourceGroup ?? TestAzureResources.CreateTestResourceGroupResource(),
            tenant ?? TestAzureResources.CreateTestTenantResource(),
            location ?? AzureLocation.WestUS2,
            principal ?? new UserPrincipal(Guid.NewGuid(), "test@example.com"),
            userSecrets ?? new JsonObject());
    }
    
    // Factory methods for test implementations of provisioning services interfaces
    public static IArmClientProvider CreateArmClientProvider() => new TestArmClientProvider();
    public static ITokenCredentialProvider CreateTokenCredentialProvider() => new TestTokenCredentialProvider();
    public static ISecretClientProvider CreateSecretClientProvider() => new TestSecretClientProvider(CreateTokenCredentialProvider());
    public static IBicepCompiler CreateBicepCompiler() => new TestBicepCompiler();
    public static IUserSecretsManager CreateUserSecretsManager() => new TestUserSecretsManager();
    public static IUserPrincipalProvider CreateUserPrincipalProvider() => new TestUserPrincipalProvider();
    public static TokenCredential CreateTokenCredential() => new TestTokenCredential();
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
/// Test implementation that provides instances for testing.
/// </summary>
internal static class TestAzureResources
{
    public static ArmClient CreateTestArmClient() => new TestArmClient();
    
    public static SubscriptionResource CreateTestSubscriptionResource()
    {
        // Create a proper subscription resource using ResourceManagerModelFactory
        var subscriptionData = TestSubscriptionData.Instance;
        return CreateSubscriptionResourceFromData(subscriptionData);
    }
    
    public static ResourceGroupResource CreateTestResourceGroupResource()
    {
        // Create a proper resource group resource using ResourceManagerModelFactory  
        var resourceGroupData = TestResourceGroupData.Instance;
        return CreateResourceGroupResourceFromData(resourceGroupData);
    }
    
    public static TenantResource CreateTestTenantResource()
    {
        // Create a proper tenant resource using ResourceManagerModelFactory
        var tenantData = TestTenantData.Instance;
        return CreateTenantResourceFromData(tenantData);
    }

    // Helper methods using reflection to create Azure SDK resources
    private static SubscriptionResource CreateSubscriptionResourceFromData(SubscriptionData data)
    {
        // Use reflection to create the resource with proper initialization
        var ctor = typeof(SubscriptionResource).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 2);
        
        if (ctor != null)
        {
            return (SubscriptionResource)ctor.Invoke([new TestArmClient(), data]);
        }
        
        throw new InvalidOperationException("Could not create SubscriptionResource");
    }

    private static ResourceGroupResource CreateResourceGroupResourceFromData(ResourceGroupData data)
    {
        // Use reflection to create the resource with proper initialization
        var ctor = typeof(ResourceGroupResource).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 2);
        
        if (ctor != null)
        {
            return (ResourceGroupResource)ctor.Invoke([new TestArmClient(), data]);
        }
        
        throw new InvalidOperationException("Could not create ResourceGroupResource");
    }

    private static TenantResource CreateTenantResourceFromData(TenantData data)
    {
        // Use reflection to create the resource with proper initialization
        var ctor = typeof(TenantResource).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length == 2);
        
        if (ctor != null)
        {
            return (TenantResource)ctor.Invoke([new TestArmClient(), data]);
        }
        
        throw new InvalidOperationException("Could not create TenantResource");
    }
}

/// <summary>
/// Test implementation of ArmClient that returns test resources.
/// </summary>
internal sealed class TestArmClient : ArmClient
{
    public TestArmClient() : base(new TestTokenCredential(), "12345678-1234-1234-1234-123456789012")
    {
    }

    public override SubscriptionResource GetDefaultSubscription(CancellationToken cancellationToken = default)
    {
        return TestAzureResources.CreateTestSubscriptionResource();
    }

    public override Task<SubscriptionResource> GetDefaultSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetDefaultSubscription(cancellationToken));
    }
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
internal sealed class TestArmDeploymentResource
{
    public static ArmDeploymentResource Create(string name)
    {
        throw new NotSupportedException("Creating test ArmDeploymentResource requires using Azure SDK test helpers");
    }
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
    protected override bool TryGetHeader(string name, out string? value)
    {
        value = default;
        return false;
    }
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string>? values)
    {
        values = default;
        return false;
    }
    public override void Dispose() { }
}

internal sealed class TestArmClientProvider : IArmClientProvider
{
    public ArmClient GetArmClient(TokenCredential credential, string subscriptionId)
    {
        return TestAzureResources.CreateTestArmClient();
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

/// <summary>
/// Test data for SubscriptionResource using Azure SDK ResourceManagerModelFactory.
/// </summary>
internal static class TestSubscriptionData
{
    public static SubscriptionData Instance { get; } = ResourceManagerModelFactory.SubscriptionData(
        id: ResourceIdentifier.Parse("/subscriptions/12345678-1234-1234-1234-123456789012"),
        subscriptionId: "12345678-1234-1234-1234-123456789012", 
        displayName: "Test Subscription",
        tenantId: new Guid("87654321-4321-4321-4321-210987654321"));
}

/// <summary>
/// Test data for ResourceGroupResource using Azure SDK ResourceManagerModelFactory.
/// </summary>
internal static class TestResourceGroupData
{
    public static ResourceGroupData Instance { get; } = ResourceManagerModelFactory.ResourceGroupData(
        id: ResourceIdentifier.Parse("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/test-rg"),
        name: "test-rg",
        resourceType: "Microsoft.Resources/resourceGroups",
        location: AzureLocation.WestUS2);
}

/// <summary>
/// Test data for TenantResource using Azure SDK ResourceManagerModelFactory.
/// </summary>
internal static class TestTenantData
{
    public static TenantData Instance { get; } = ResourceManagerModelFactory.TenantData(
        id: ResourceIdentifier.Parse("/providers/Microsoft.Resources/tenants/87654321-4321-4321-4321-210987654321"),
        tenantId: new Guid("87654321-4321-4321-4321-210987654321"),
        displayName: "Test Tenant", 
        defaultDomain: "testdomain.onmicrosoft.com");
}