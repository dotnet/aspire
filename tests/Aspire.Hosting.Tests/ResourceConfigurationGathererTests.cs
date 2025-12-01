// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECERTIFICATES001

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Tests;

public class ResourceConfigurationGathererTests
{
    #region ResourceArgumentsConfigurationGatherer Tests

    [Fact]
    public async Task ResourceArgumentsConfigurationGatherer_WithCommandLineArgsCallback_GathersArguments()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddExecutable("test", "test.exe", ".")
            .WithArgs("arg1", "arg2")
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceArgumentsConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Equal(2, context.Arguments.Count);
        Assert.Equal("arg1", context.Arguments[0]);
        Assert.Equal("arg2", context.Arguments[1]);
    }

    [Fact]
    public async Task ResourceArgumentsConfigurationGatherer_WithMultipleCallbacks_GathersAllArguments()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddExecutable("test", "test.exe", ".")
            .WithArgs("arg1")
            .WithArgs(ctx => ctx.Args.Add("arg2"))
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceArgumentsConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Equal(2, context.Arguments.Count);
        Assert.Equal("arg1", context.Arguments[0]);
        Assert.Equal("arg2", context.Arguments[1]);
    }

    [Fact]
    public async Task ResourceArgumentsConfigurationGatherer_NoArgsAnnotations_DoesNothing()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddExecutable("test", "test.exe", ".").Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceArgumentsConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Empty(context.Arguments);
    }

    [Fact]
    public async Task ResourceArgumentsConfigurationGatherer_AsyncCallback_ExecutesCorrectly()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddExecutable("test", "test.exe", ".")
            .WithArgs(async ctx =>
            {
                await Task.Delay(1);
                ctx.Args.Add("async-arg");
            })
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceArgumentsConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Single(context.Arguments);
        Assert.Equal("async-arg", context.Arguments[0]);
    }

    #endregion

    #region ResourceEnvironmentVariablesConfigurationGatherer Tests

    [Fact]
    public async Task ResourceEnvironmentVariablesConfigurationGatherer_WithEnvironmentCallback_GathersEnvironmentVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image")
            .WithEnvironment("KEY1", "value1")
            .WithEnvironment("KEY2", "value2")
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceEnvironmentVariablesConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Equal(2, context.EnvironmentVariables.Count);
        Assert.Equal("value1", context.EnvironmentVariables["KEY1"]);
        Assert.Equal("value2", context.EnvironmentVariables["KEY2"]);
    }

    [Fact]
    public async Task ResourceEnvironmentVariablesConfigurationGatherer_WithMultipleCallbacks_GathersAllVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image")
            .WithEnvironment("KEY1", "value1")
            .WithEnvironment(ctx => ctx.EnvironmentVariables["KEY2"] = "value2")
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceEnvironmentVariablesConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Equal(2, context.EnvironmentVariables.Count);
        Assert.Equal("value1", context.EnvironmentVariables["KEY1"]);
        Assert.Equal("value2", context.EnvironmentVariables["KEY2"]);
    }

    [Fact]
    public async Task ResourceEnvironmentVariablesConfigurationGatherer_NoEnvironmentAnnotations_DoesNothing()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image").Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceEnvironmentVariablesConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Empty(context.EnvironmentVariables);
    }

    [Fact]
    public async Task ResourceEnvironmentVariablesConfigurationGatherer_AsyncCallback_ExecutesCorrectly()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image")
            .WithEnvironment(async ctx =>
            {
                await Task.Delay(1);
                ctx.EnvironmentVariables["ASYNC_KEY"] = "async-value";
            })
            .Resource;

        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceEnvironmentVariablesConfigurationGatherer();

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Single(context.EnvironmentVariables);
        Assert.Equal("async-value", context.EnvironmentVariables["ASYNC_KEY"]);
    }

    #endregion

    #region ResourceCertificateTrustConfigurationGatherer Tests

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceCertificateTrustConfigurationGatherer_WithCertificateAuthorityCollection_SetsEnvironmentVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var caCollection = builder.AddCertificateAuthorityCollection("test-ca").WithCertificate(cert);

        var resource = builder.AddContainer("test", "image")
            .WithCertificateAuthorityCollection(caCollection)
            .Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Contains("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
        var metadata = context.Metadata.OfType<CertificateTrustConfigurationMetadata>().Single();
        Assert.Equal(CertificateTrustScope.Append, metadata.Scope);
        Assert.NotEmpty(metadata.Certificates);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceCertificateTrustConfigurationGatherer_WithSystemScope_IncludesSystemCertificates()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var caCollection = builder.AddCertificateAuthorityCollection("test-ca").WithCertificate(cert);

        var resource = builder.AddContainer("test", "image")
            .WithCertificateAuthorityCollection(caCollection)
            .WithCertificateTrustScope(CertificateTrustScope.System)
            .Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Contains("SSL_CERT_FILE", context.EnvironmentVariables.Keys);
        Assert.Contains("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
        var metadata = context.Metadata.OfType<CertificateTrustConfigurationMetadata>().Single();
        Assert.Equal(CertificateTrustScope.System, metadata.Scope);
        // System scope should include system root certificates
        Assert.True(metadata.Certificates.Count > 1);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceCertificateTrustConfigurationGatherer_WithOverrideScope_SetsCorrectEnvironmentVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var caCollection = builder.AddCertificateAuthorityCollection("test-ca").WithCertificate(cert);

        var resource = builder.AddContainer("test", "image")
            .WithCertificateAuthorityCollection(caCollection)
            .WithCertificateTrustScope(CertificateTrustScope.Override)
            .Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Contains("SSL_CERT_FILE", context.EnvironmentVariables.Keys);
        Assert.Contains("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
        var metadata = context.Metadata.OfType<CertificateTrustConfigurationMetadata>().Single();
        Assert.Equal(CertificateTrustScope.Override, metadata.Scope);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceCertificateTrustConfigurationGatherer_WithNoneScope_DoesNotSetEnvironmentVariables()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var caCollection = builder.AddCertificateAuthorityCollection("test-ca").WithCertificate(cert);

        var resource = builder.AddContainer("test", "image")
            .WithCertificateAuthorityCollection(caCollection)
            .WithCertificateTrustScope(CertificateTrustScope.None)
            .Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.DoesNotContain("SSL_CERT_FILE", context.EnvironmentVariables.Keys);
        Assert.DoesNotContain("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
        var metadata = context.Metadata.OfType<CertificateTrustConfigurationMetadata>().Single();
        Assert.Equal(CertificateTrustScope.None, metadata.Scope);
    }

    [Fact]
    public async Task ResourceCertificateTrustConfigurationGatherer_NoCertificateAnnotation_DoesNothing()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image").Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.DoesNotContain("SSL_CERT_FILE", context.EnvironmentVariables.Keys);
        Assert.DoesNotContain("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceCertificateTrustConfigurationGatherer_WithAppendScope_DoesNotSetSSL_CERT_FILE()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var caCollection = builder.AddCertificateAuthorityCollection("test-ca").WithCertificate(cert);

        var resource = builder.AddContainer("test", "image")
            .WithCertificateAuthorityCollection(caCollection)
            .WithCertificateTrustScope(CertificateTrustScope.Append)
            .Resource;

        var configContextFactory = CreateCertificateTrustConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceCertificateTrustConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.DoesNotContain("SSL_CERT_FILE", context.EnvironmentVariables.Keys);
        Assert.Contains("SSL_CERT_DIR", context.EnvironmentVariables.Keys);
    }

    #endregion

    #region ResourceServerAuthenticationCertificateConfigurationGatherer Tests

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_WithCertificate_ConfiguresMetadata()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();

        var resource = builder.AddContainer("test", "image")
            .WithAnnotation(new ServerAuthenticationCertificateAnnotation { Certificate = cert })
            .Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        var metadata = context.Metadata.OfType<ServerAuthenticationCertificateConfigurationMetadata>().Single();
        Assert.Equal(cert, metadata.Certificate);
        Assert.NotNull(metadata.KeyPathReference);
        Assert.NotNull(metadata.PfxPathReference);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_WithPassword_StoresPassword()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:password"] = "test-password";
        var cert = CreateTestCertificate();
        var password = builder.AddParameter("password", secret: true);

        var resource = builder.AddContainer("test", "image")
            .WithAnnotation(new ServerAuthenticationCertificateAnnotation
            {
                Certificate = cert,
                Password = password.Resource
            })
            .Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        var metadata = context.Metadata.OfType<ServerAuthenticationCertificateConfigurationMetadata>().Single();
        Assert.NotNull(metadata.Password);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_WithUseDeveloperCertificate_UsesDeveloperCert()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Configure developer certificate service
        var devCert = CreateTestCertificate();
        builder.Services.AddSingleton<IDeveloperCertificateService>(new TestDeveloperCertificateService(devCert));

        var resource = builder.AddContainer("test", "image")
            .WithAnnotation(new ServerAuthenticationCertificateAnnotation
            {
                UseDeveloperCertificate = true
            })
            .Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext, builder.Services.BuildServiceProvider());
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        var metadata = context.Metadata.OfType<ServerAuthenticationCertificateConfigurationMetadata>().Single();
        Assert.Equal(devCert, metadata.Certificate);
    }

    [Fact]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_NoCertificateAnnotation_DoesNothing()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resource = builder.AddContainer("test", "image").Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.Empty(context.Metadata.OfType<ServerAuthenticationCertificateConfigurationMetadata>());
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_TracksReferenceUsage()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();

        var resource = builder.AddContainer("test", "image")
            .WithAnnotation(new ServerAuthenticationCertificateAnnotation { Certificate = cert })
            .Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        var metadata = context.Metadata.OfType<ServerAuthenticationCertificateConfigurationMetadata>().Single();

        // Initially, references should not be resolved
        Assert.False(metadata.IsKeyPathReferenced);
        Assert.False(metadata.IsPfxPathReferenced);

        // Accessing the references should mark them as resolved
        _ = await metadata.KeyPathReference.GetValueAsync(CancellationToken.None);
        Assert.True(metadata.IsKeyPathReferenced);
        Assert.False(metadata.IsPfxPathReferenced);

        _ = await metadata.PfxPathReference.GetValueAsync(CancellationToken.None);
        Assert.True(metadata.IsPfxPathReferenced);
    }

    [Fact]
    [RequiresCertificateStoreAccess]
    public async Task ResourceServerAuthenticationCertificateConfigurationGatherer_WithCallback_ExecutesCallback()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var cert = CreateTestCertificate();
        var callbackExecuted = false;

        var resource = builder.AddContainer("test", "image")
            .WithAnnotation(new ServerAuthenticationCertificateAnnotation { Certificate = cert })
            .WithAnnotation(new ServerAuthenticationCertificateConfigurationCallbackAnnotation(ctx =>
            {
                callbackExecuted = true;
                return Task.CompletedTask;
            }))
            .Resource;

        var configContextFactory = CreateServerAuthenticationCertificateConfigurationContextFactory();
        var context = CreateGathererContext(resource, builder.ExecutionContext);
        var gatherer = new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory);

        // Act
        await gatherer.GatherAsync(context);

        // Assert
        Assert.True(callbackExecuted);
    }

    #endregion

    #region Helper Methods

    private static ResourceConfigurationGathererContext CreateGathererContext(
        IResource resource,
        DistributedApplicationExecutionContext executionContext,
        IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= CreateDefaultServiceProvider();
        var options = new DistributedApplicationExecutionContextOptions(executionContext.Operation)
        {
            ServiceProvider = serviceProvider
        };
        var execContext = new DistributedApplicationExecutionContext(options);

        return new ResourceConfigurationGathererContext
        {
            Resource = resource,
            ResourceLogger = NullLogger.Instance,
            ExecutionContext = execContext
        };
    }

    private static IServiceProvider CreateDefaultServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDeveloperCertificateService>(new TestDeveloperCertificateService());
        return services.BuildServiceProvider();
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName("CN=test"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }

    private static Func<CertificateTrustScope, CertificateTrustConfigurationContext> CreateCertificateTrustConfigurationContextFactory()
    {
        return scope => new CertificateTrustConfigurationContext
        {
            CertificateBundlePath = ReferenceExpression.Create($"/etc/ssl/certs/ca-bundle.crt"),
            CertificateDirectoriesPath = ReferenceExpression.Create($"/etc/ssl/certs")
        };
    }

    private static Func<X509Certificate2, ServerAuthenticationCertificateConfigurationContext> CreateServerAuthenticationCertificateConfigurationContextFactory()
    {
        return cert => new ServerAuthenticationCertificateConfigurationContext
        {
            CertificatePath = ReferenceExpression.Create($"/etc/ssl/certs/server.crt"),
            KeyPath = ReferenceExpression.Create($"/etc/ssl/private/server.key"),
            PfxPath = ReferenceExpression.Create($"/etc/ssl/certs/server.pfx")
        };
    }

    private sealed class TestDeveloperCertificateService : IDeveloperCertificateService
    {
        private readonly X509Certificate2? _certificate;

        public TestDeveloperCertificateService(X509Certificate2? certificate = null)
        {
            _certificate = certificate;
        }

        public ImmutableList<X509Certificate2> Certificates =>
            _certificate != null ? ImmutableList.Create(_certificate) : ImmutableList<X509Certificate2>.Empty;

        public bool SupportsContainerTrust => true;

        public bool TrustCertificate => true;

        public bool UseForServerAuthentication => true;
    }

    #endregion
}
