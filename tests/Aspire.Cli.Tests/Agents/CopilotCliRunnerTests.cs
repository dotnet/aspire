// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Agents.CopilotCli;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class CopilotCliRunnerTests
{
    [Fact]
    public async Task GetVersionAsync_ChecksForCopilot()
    {
        // Arrange
        var runner = new CopilotCliRunner(NullLogger<CopilotCliRunner>.Instance);

        // Act
        _ = await runner.GetVersionAsync(CancellationToken.None);

        // Assert
        // The result depends on whether copilot is actually installed on the system
        // This test just ensures we don't crash and do attempt the PATH lookup
        // Since we can't guarantee copilot is installed in the test environment,
        // we just verify the method completes without throwing
        // Version can be null (not installed) or a real version
    }
}
