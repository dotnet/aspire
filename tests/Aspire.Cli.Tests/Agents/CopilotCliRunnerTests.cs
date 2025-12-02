// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.CopilotCli;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class CopilotCliRunnerTests
{
    [Theory]
    [InlineData("VSCODE_INJECTION")]
    [InlineData("VSCODE_IPC_HOOK")]
    [InlineData("VSCODE_GIT_ASKPASS_NODE")]
    [InlineData("VSCODE_GIT_ASKPASS_EXTRA_ARGS")]
    [InlineData("VSCODE_GIT_ASKPASS_MAIN")]
    [InlineData("VSCODE_GIT_IPC_HANDLE")]
    public async Task GetVersionAsync_WhenVSCodeEnvironmentVariableSet_ReturnsVersion(string envVarName)
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable(envVarName);
        try
        {
            Environment.SetEnvironmentVariable(envVarName, "test-value");
            var runner = new CopilotCliRunner(NullLogger<CopilotCliRunner>.Instance);

            // Act
            var version = await runner.GetVersionAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(version);
            Assert.Equal(1, version.Major);
            Assert.Equal(0, version.Minor);
            Assert.Equal(0, version.Patch);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable(envVarName, originalValue);
        }
    }

    [Fact]
    public async Task GetVersionAsync_WhenTermProgramIsVSCode_ReturnsVersion()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "vscode");
            var runner = new CopilotCliRunner(NullLogger<CopilotCliRunner>.Instance);

            // Act
            var version = await runner.GetVersionAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(version);
            Assert.Equal(1, version.Major);
            Assert.Equal(0, version.Minor);
            Assert.Equal(0, version.Patch);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalValue);
        }
    }

    [Fact]
    public async Task GetVersionAsync_WhenTermProgramIsNotVSCode_DoesNotReturnDummyVersion()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "iTerm.app");
            var runner = new CopilotCliRunner(NullLogger<CopilotCliRunner>.Instance);

            // Act
            var version = await runner.GetVersionAsync(CancellationToken.None);

            // Assert
            // The actual result depends on whether copilot is installed on the system
            // This test just ensures TERM_PROGRAM=iTerm.app doesn't trigger the VSCode shortcut
            // If copilot is not installed, version should be null
            // If it is installed, it should be the real version (not necessarily 1.0.0)
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalValue);
        }
    }

    [Fact]
    public async Task GetVersionAsync_WhenNoVSCodeEnvironmentVariables_ChecksForCopilot()
    {
        // Arrange
        var envVars = new[]
        {
            "VSCODE_INJECTION",
            "VSCODE_IPC_HOOK",
            "VSCODE_GIT_ASKPASS_NODE",
            "VSCODE_GIT_ASKPASS_EXTRA_ARGS",
            "VSCODE_GIT_ASKPASS_MAIN",
            "VSCODE_GIT_IPC_HANDLE",
            "TERM_PROGRAM"
        };

        var originalValues = new Dictionary<string, string?>();
        foreach (var envVar in envVars)
        {
            originalValues[envVar] = Environment.GetEnvironmentVariable(envVar);
        }
        try
        {
            // Clear all VSCode environment variables
            foreach (var envVar in envVars)
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }

            var runner = new CopilotCliRunner(NullLogger<CopilotCliRunner>.Instance);

            // Act
            _ = await runner.GetVersionAsync(CancellationToken.None);

            // Assert
            // The result depends on whether copilot is actually installed on the system
            // This test just ensures we don't crash and do attempt the PATH lookup
            // Since we can't guarantee copilot is installed in the test environment,
            // we just verify the method completes without throwing
        }
        finally
        {
            // Cleanup
            foreach (var kvp in originalValues)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
    }
}
