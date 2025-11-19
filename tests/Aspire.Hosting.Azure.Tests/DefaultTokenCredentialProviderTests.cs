// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES002

using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultTokenCredentialProviderTests
{
    [Fact]
    public void Constructor_PublishMode_NoCredentialSource_UsesAzureCli()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: null);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<AzureCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_NoCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: null);
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_PublishMode_ExplicitDefaultCredentialSource_UsesAzureCli()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "Default");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<AzureCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_ExplicitDefaultCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "Default");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_PublishMode_ExplicitNonDefaultCredentialSource_RespectsSource()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzurePowerShell");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<AzurePowerShellCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_ExplicitCredentialSource_RespectsSource()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "VisualStudio");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<VisualStudioCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_InvalidCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "InvalidSource");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_AzureCliCredentialSource_UsesAzureCliCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzureCli");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<AzureCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_AzureDeveloperCliCredentialSource_UsesAzureDeveloperCliCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzureDeveloperCli");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<AzureDeveloperCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_RunMode_InteractiveBrowserCredentialSource_UsesInteractiveBrowserCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "InteractiveBrowser");
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions,
            executionContext);

        // Assert
        Assert.IsType<InteractiveBrowserCredential>(provider.TokenCredential);
    }

    private static IOptions<AzureProvisionerOptions> CreateAzureOptions(string? credentialSource)
    {
        var options = new AzureProvisionerOptions
        {
            // Intentionally allow CredentialSource to be null for tests that verify null handling.
            CredentialSource = credentialSource!
        };
        return Options.Create(options);
    }

}
