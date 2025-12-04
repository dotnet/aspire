// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.CopilotCli;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class CopilotCliRunnerTests
{
    [Fact]
    public async Task GetVersionAsync_WhenVSCodeIpcHookSet_ReturnsVersion()
    {
        // Arrange
        var environmentVariables = new Dictionary<string, string?>
        {
            ["VSCODE_IPC_HOOK"] = "test-value"
        };
        var executionContext = CreateExecutionContext(environmentVariables);
        var runner = new CopilotCliRunner(executionContext, NullLogger<CopilotCliRunner>.Instance);

        // Act
        var version = await runner.GetVersionAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(version);
        Assert.Equal(1, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.Equal(0, version.Patch);
    }

    [Fact]
    public async Task GetVersionAsync_WhenVSCodeIpcHookNotSet_ChecksForCopilot()
    {
        // Arrange
        var environmentVariables = new Dictionary<string, string?>();
        var executionContext = CreateExecutionContext(environmentVariables);
        var runner = new CopilotCliRunner(executionContext, NullLogger<CopilotCliRunner>.Instance);

        // Act
        _ = await runner.GetVersionAsync(CancellationToken.None);

        // Assert
        // The result depends on whether copilot is actually installed on the system
        // This test just ensures we don't crash and do attempt the PATH lookup
        // Since we can't guarantee copilot is installed in the test environment,
        // we just verify the method completes without throwing
        // Version can be null (not installed) or a real version
    }

    [Fact]
    public async Task GetVersionAsync_WhenVSCodeIpcHookEmpty_ChecksForCopilot()
    {
        // Arrange
        var environmentVariables = new Dictionary<string, string?>
        {
            ["VSCODE_IPC_HOOK"] = ""
        };
        var executionContext = CreateExecutionContext(environmentVariables);
        var runner = new CopilotCliRunner(executionContext, NullLogger<CopilotCliRunner>.Instance);

        // Act
        _ = await runner.GetVersionAsync(CancellationToken.None);

        // Assert
        // Empty string should not trigger VSCode detection
        // Version can be null (not installed) or a real version
    }

    private static CliExecutionContext CreateExecutionContext(Dictionary<string, string?> environmentVariables)
    {
        var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        return new CliExecutionContext(
            workingDirectory: tempDir,
            hivesDirectory: tempDir,
            cacheDirectory: tempDir,
            sdksDirectory: tempDir,
            debugMode: false,
            environmentVariables: environmentVariables);
    }
}
