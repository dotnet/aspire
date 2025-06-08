// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure.Core;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultUserPrincipalProviderTests
{
    [Fact]
    public async Task GetUserPrincipalAsync_ReturnsValidUserPrincipal()
    {
        // Arrange
        var tokenCredentialProvider = TestProvisioningServices.CreateTokenCredentialProvider();
        var provider = new DefaultUserPrincipalProvider(tokenCredentialProvider);

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.NotNull(principal);
        Assert.NotEqual(Guid.Empty, principal.Id);
        Assert.NotNull(principal.Name);
    }

    [Fact]
    public async Task GetUserPrincipalAsync_ParsesTokenCorrectly()
    {
        // Arrange
        var expectedOid = Guid.NewGuid();
        var expectedUpn = "test@example.com";
        var token = CreateTestToken(expectedOid, expectedUpn);
        var tokenCredentialProvider = new TestTokenCredentialProviderWithCustomToken(token);
        var provider = new DefaultUserPrincipalProvider(tokenCredentialProvider);

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.Equal(expectedOid, principal.Id);
        Assert.Equal(expectedUpn, principal.Name);
    }

    [Fact]
    public async Task GetUserPrincipalAsync_ParsesTokenWithEmail()
    {
        // Arrange
        var expectedOid = Guid.NewGuid();
        var expectedEmail = "user@company.com";
        var token = CreateTestTokenWithEmail(expectedOid, expectedEmail);
        var tokenCredentialProvider = new TestTokenCredentialProviderWithCustomToken(token);
        var provider = new DefaultUserPrincipalProvider(tokenCredentialProvider);

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.Equal(expectedOid, principal.Id);
        Assert.Equal(expectedEmail, principal.Name);
    }

    [Fact]
    public async Task GetUserPrincipalAsync_HandlesCancellation()
    {
        // Arrange
        var tokenCredentialProvider = TestProvisioningServices.CreateTokenCredentialProvider();
        var provider = new DefaultUserPrincipalProvider(tokenCredentialProvider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.GetUserPrincipalAsync(cts.Token));
    }

    [Fact]
    public async Task GetUserPrincipalAsync_RespectsTokenCredentialProviderDependency()
    {
        // Arrange
        var customTokenCredential = new TestTokenCredential();
        var tokenCredentialProvider = new TestTokenCredentialProviderWithCredential(customTokenCredential);
        var provider = new DefaultUserPrincipalProvider(tokenCredentialProvider);

        // Act
        var principal = await provider.GetUserPrincipalAsync();

        // Assert
        Assert.NotNull(principal);
        // Verify the provider used the injected token credential
        Assert.True(customTokenCredential.GetTokenCalled);
    }

    private static string CreateTestToken(Guid oid, string upn)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
        
        var payload = new
        {
            oid = oid.ToString(),
            upn = upn,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"));

        return $"{header}.{payloadBase64}.{signature}";
    }

    private static string CreateTestTokenWithEmail(Guid oid, string email)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
        
        var payload = new
        {
            oid = oid.ToString(),
            email = email,
            exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"));

        return $"{header}.{payloadBase64}.{signature}";
    }

    private sealed class TestTokenCredentialProviderWithCustomToken(string token) : ITokenCredentialProvider
    {
        public TokenCredential GetTokenCredential() => new TestTokenCredentialWithCustomToken(token);
    }

    private sealed class TestTokenCredentialWithCustomToken(string token) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            return new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1)));
        }
    }

    private sealed class TestTokenCredentialProviderWithCredential(TestTokenCredential tokenCredential) : ITokenCredentialProvider
    {
        public TokenCredential GetTokenCredential() => tokenCredential;
    }

    private sealed class TestTokenCredential : TokenCredential
    {
        public bool GetTokenCalled { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            GetTokenCalled = true;
            var oid = Guid.NewGuid();
            var upn = "test@example.com";
            var token = CreateTestToken(oid, upn);
            return new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            GetTokenCalled = true;
            cancellationToken.ThrowIfCancellationRequested();
            var oid = Guid.NewGuid();
            var upn = "test@example.com";
            var token = CreateTestToken(oid, upn);
            return ValueTask.FromResult(new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1)));
        }

        private static string CreateTestToken(Guid oid, string upn)
        {
            var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT" })));
            
            var payload = new
            {
                oid = oid.ToString(),
                upn = upn,
                exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-signature"));

            return $"{header}.{payloadBase64}.{signature}";
        }
    }
}