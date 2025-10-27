// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDEPLOYMENT001

using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultTokenCredentialProviderTests
{
    [Fact]
    public void Constructor_NoCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: null);

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_ExplicitDefaultCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "Default");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_ExplicitNonDefaultCredentialSource_RespectsSource()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzurePowerShell");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<AzurePowerShellCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_ExplicitCredentialSource_RespectsSource()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "VisualStudio");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<VisualStudioCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_InvalidCredentialSource_UsesDefaultAzureCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "InvalidSource");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<DefaultAzureCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_AzureCliCredentialSource_UsesAzureCliCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzureCli");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<AzureCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_AzureDeveloperCliCredentialSource_UsesAzureDeveloperCliCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "AzureDeveloperCli");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<AzureDeveloperCliCredential>(provider.TokenCredential);
    }

    [Fact]
    public void Constructor_InteractiveBrowserCredentialSource_UsesInteractiveBrowserCredential()
    {
        // Arrange
        var azureOptions = CreateAzureOptions(credentialSource: "InteractiveBrowser");

        // Act
        var provider = new DefaultTokenCredentialProvider(
            NullLogger<DefaultTokenCredentialProvider>.Instance,
            azureOptions);

        // Assert
        Assert.IsType<InteractiveBrowserCredential>(provider.TokenCredential);
    }

    private static IOptions<AzureProvisionerOptions> CreateAzureOptions(string? credentialSource)
    {
        var options = new AzureProvisionerOptions
        {
            CredentialSource = credentialSource ?? "Default"
        };
        return Options.Create(options);
    }

}
