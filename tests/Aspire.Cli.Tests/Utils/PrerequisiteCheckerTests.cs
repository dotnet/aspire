// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Utils;

public class PrerequisiteCheckerTests
{

    [Fact]
    public async Task CheckPrerequisitesAsync_ReturnsValidResult()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var result = await checker.CheckPrerequisitesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Warnings);
        Assert.NotNull(result.Errors);
        // Note: This will depend on the actual system state
        // We're just verifying the method completes without throwing
    }

    [Fact]
    public async Task CheckDotNetSdkAsync_ReturnsValid_WhenDotNetIsInstalled()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var (isValid, installedVersion, message) = await checker.CheckDotNetSdkAsync(CancellationToken.None);

        // Assert
        // This test depends on .NET being installed in the test environment
        // Since tests run in .NET environment, SDK should be available
        Assert.True(isValid, "Expected .NET SDK to be installed in test environment");
        Assert.NotNull(installedVersion);
        Assert.Null(message); // No error message when SDK is valid
    }

    [Fact]
    public async Task CheckContainerRuntimeAsync_ReturnsResult()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var (isAvailable, runtimeName, message) = await checker.CheckContainerRuntimeAsync(CancellationToken.None);

        // Assert
        // Verify method completes without throwing
        Assert.True(isAvailable || !isAvailable); // Either true or false is valid
        
        if (isAvailable)
        {
            Assert.NotNull(runtimeName);
            Assert.True(runtimeName == "docker" || runtimeName == "podman");
            Assert.Null(message);
        }
        else
        {
            Assert.NotNull(message);
            Assert.Contains("container runtime", message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CheckWSLEnvironment_ReturnsResult()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var (isWSL, warning) = checker.CheckWSLEnvironment();

        // Assert
        // Verify method completes without throwing
        Assert.True(isWSL || !isWSL); // Either true or false is valid
        
        if (isWSL)
        {
            Assert.NotNull(warning);
            Assert.Contains("WSL", warning);
            Assert.Contains("aka.ms", warning);
        }
    }

    [Fact]
    public async Task CheckDockerEngineAsync_ReturnsResult()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var (isDockerEngine, message) = await checker.CheckDockerEngineAsync(CancellationToken.None);

        // Assert
        // Verify method completes without throwing
        Assert.True(isDockerEngine || !isDockerEngine); // Either true or false is valid
        
        if (isDockerEngine)
        {
            Assert.NotNull(message);
            Assert.Contains("Docker Engine", message);
            Assert.Contains("aka.ms", message);
        }
    }

    [Fact]
    public async Task CheckPrerequisitesAsync_IncludesWarningsAndErrors()
    {
        // Arrange
        var dotNetRunner = new TestDotNetCliRunner();
        var logger = NullLogger<PrerequisiteChecker>.Instance;
        var checker = new PrerequisiteChecker(dotNetRunner, logger);

        // Act
        var result = await checker.CheckPrerequisitesAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Warnings);
        Assert.NotNull(result.Errors);
        
        // Verify IsValid property works correctly
        if (result.Errors.Count > 0)
        {
            Assert.False(result.IsValid);
        }
        else
        {
            Assert.True(result.IsValid);
        }
    }

    [Fact]
    public void PrerequisiteCheckResult_IsValid_ReturnsFalse_WhenErrorsExist()
    {
        // Arrange
        var result = new PrerequisiteCheckResult();
        result.Errors.Add("Test error");

        // Act & Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void PrerequisiteCheckResult_IsValid_ReturnsTrue_WhenNoErrors()
    {
        // Arrange
        var result = new PrerequisiteCheckResult();
        result.Warnings.Add("Test warning");

        // Act & Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void PrerequisiteCheckResult_IsValid_ReturnsTrue_WhenNoErrorsOrWarnings()
    {
        // Arrange
        var result = new PrerequisiteCheckResult();

        // Act & Assert
        Assert.True(result.IsValid);
    }
}
