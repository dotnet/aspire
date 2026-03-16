// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class ProcessGuestLauncherTests
{
    [Fact]
    public async Task LaunchAsync_ForwardsStdoutToLiveCallback()
    {
        // Arrange
        var forwardedLines = new List<(OutputLineStream Stream, string Line)>();
        var launcher = new ProcessGuestLauncher(
            "typescript",
            NullLogger.Instance,
            liveOutputCallback: (stream, line) => forwardedLines.Add((stream, line)));

        // Act
        var (exitCode, output) = await launcher.LaunchAsync(
            "dotnet",
            ["--version"],
            new DirectoryInfo(Path.GetTempPath()),
            new Dictionary<string, string>(),
            CancellationToken.None);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.NotNull(output);
        Assert.Contains(forwardedLines, line => line.Stream == OutputLineStream.StdOut && !string.IsNullOrWhiteSpace(line.Line));
    }
}
