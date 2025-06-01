// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureKeyVaultTests
{
    [Fact]
    public async Task AddKeyVaultViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mykv = builder.AddAzureKeyVault("mykv");

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(mykv.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
              
    }

    [Fact]
    public async Task AddKeyVaultViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var mykv = builder.AddAzureKeyVault("mykv");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(model, mykv.Resource);
        var kvRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "mykv-roles");
        var (kvRolesManifest, kvRolesBicep) = await AzureManifestUtils.GetManifestWithBicep(kvRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(kvRolesBicep, "bicep")
              .AppendContentAsFile(kvRolesManifest.ToString(), "json");
              
    }

    [Fact]
    public async Task WithEnvironment_AddsKeyVaultSecretReference()
    {
        // Arrange: Create a test application builder.
        using var builder = TestDistributedApplicationBuilder.Create();

        // Add a key vault resource.
        var kv = builder.AddAzureKeyVault("myKeyVault");

        kv.Resource.SecretResolver = (s, ct) =>
        {
            return Task.FromResult<string?>("my secret value");
        };

        // Get a secret reference from the key vault resource.
        var secretReference = kv.Resource.GetSecret("mySecret");

        // Add a container resource that supports environment variables.
        var containerBuilder = builder.AddContainer("myContainer", "nginx")
                                       .WithEnvironment("MY_SECRET", secretReference);

        var runEnv = await containerBuilder.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run);
        var publishEnv = await containerBuilder.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);

        var runKvp = Assert.Single(runEnv);
        var pubishKvp = Assert.Single(publishEnv);

        Assert.Equal("MY_SECRET", runKvp.Key);
        Assert.Same("my secret value", runKvp.Value);

        Assert.Equal("MY_SECRET", pubishKvp.Key);
        Assert.Equal("{myKeyVault.secrets.mySecret}", pubishKvp.Value);
    }

    [Fact]
    public async Task ConsumingAKeyVaultSecretInAnotherBicepModule()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var kv = builder.AddAzureKeyVault("myKeyVault");

        var secretReference = kv.Resource.GetSecret("mySecret");
        var secretReference2 = kv.Resource.GetSecret("mySecret2");

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            var secret = secretReference.AsKeyVaultSecret(infra);
            var secret2 = secretReference2.AsKeyVaultSecret(infra);

            // Should be idempotent
            _ = secretReference.AsKeyVaultSecret(infra);

            infra.Add(new ProvisioningOutput("secretUri1", typeof(string))
            {
                Value = secret.Properties.SecretUri
            });

            infra.Add(new ProvisioningOutput("secretUri2", typeof(string))
            {
                Value = secret2.Properties.SecretUri
            });
        });

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
              
    }

    [Fact]
    public void GetSecret_ReturnsSecretReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var kv = builder.AddAzureKeyVault("myKeyVault");
        
        var secret = kv.GetSecret("mySecret");
        
        Assert.NotNull(secret);
        Assert.Equal("mySecret", secret.SecretName);
        Assert.Same(kv.Resource, secret.Resource);
    }

    [Fact]
    public async Task WithSecret_WithParameterResource_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var secret = builder.AddParameter("my-secret-param", secret: true);
        var kv = builder.AddAzureKeyVault("mykv")
            .WithSecret("my-secret", secret);

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(kv.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task WithSecret_WithReferenceExpression_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var pwd = builder.AddParameter("password", secret: true);
        var connectionString = ReferenceExpression.Create($"Server=localhost;Database=mydb;pwd={pwd}");
        var kv = builder.AddAzureKeyVault("mykv")
            .WithSecret("connection-string", connectionString);

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(kv.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Fact]
    public async Task WithSecret_WithMultipleSecrets_GeneratesCorrectBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var secretParam = builder.AddParameter("secret-param", secret: true);
        var apiKey = builder.AddParameter("api-key", secret: true);
        var connectionString = ReferenceExpression.Create($"Server=localhost;Database=mydb;User=user");

        var kv = builder.AddAzureKeyVault("mykv")
            .WithSecret("my-secret", secretParam)
            .WithSecret("app-api-key", apiKey)
            .WithSecret("connection-string", connectionString);

        var (manifest, bicep) = await AzureManifestUtils.GetManifestWithBicep(kv.Resource);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void WithSecret_WithEmptySecretName_ThrowsArgumentException(string invalidSecretName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var secretParam = builder.AddParameter("secretParam", secret: true);
        var kv = builder.AddAzureKeyVault("myKeyVault");

        var exception = Assert.Throws<ArgumentException>(() => kv.WithSecret(invalidSecretName, secretParam));
        Assert.Contains("Secret name", exception.Message);
    }

    [Theory]
    [InlineData("secret_with_underscores")]
    [InlineData("secret.with.dots")]
    [InlineData("secret with spaces")]
    [InlineData("secret/with/slashes")]
    [InlineData("secret@with@symbols")]
    public void WithSecret_WithInvalidSecretName_ThrowsArgumentException(string invalidName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var secretParam = builder.AddParameter("secretParam", secret: true);
        var kv = builder.AddAzureKeyVault("myKeyVault");

        var exception = Assert.Throws<ArgumentException>(() => kv.WithSecret(invalidName, secretParam));
        Assert.Contains("Secret name can only contain", exception.Message);
    }

    [Theory]
    [InlineData("valid-secret")]
    [InlineData("VALID-SECRET")]
    [InlineData("valid123")]
    [InlineData("123valid")]
    [InlineData("a")]
    public void WithSecret_WithValidSecretName_DoesNotThrow(string validSecretName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var secretParam = builder.AddParameter("secretParam", secret: true);
        var kv = builder.AddAzureKeyVault("myKeyVault");

        var exception = Record.Exception(() => kv.WithSecret(validSecretName, secretParam));
        Assert.Null(exception);
    }

    [Fact]
    public void WithSecret_WithTooLongSecretName_ThrowsArgumentException()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var secretParam = builder.AddParameter("secretParam", secret: true);
        var kv = builder.AddAzureKeyVault("myKeyVault");

        // Create a 128-character secret name (too long)
        var tooLongSecretName = new string('a', 128);

        var exception = Assert.Throws<ArgumentException>(() => kv.WithSecret(tooLongSecretName, secretParam));
        Assert.Contains("cannot be longer than 127 characters", exception.Message);
    }
}
