// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Hosting.Ats;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Projects;

public class GuestRuntimeTests
{
    [Fact]
    public async Task InstallDependenciesAsync_WhenNpmIsMissing_ReturnsNodeInstallMessage()
    {
        var runtime = new GuestRuntime(
            new RuntimeSpec
            {
                Language = KnownLanguageId.TypeScript,
                DisplayName = "TypeScript (Node.js)",
                CodeGenLanguage = "typescript",
                DetectionPatterns = ["apphost.ts"],
                Execute = new CommandSpec { Command = "npx", Args = ["tsx", "{appHostFile}"] },
                InstallDependencies = new CommandSpec { Command = "npm", Args = ["install"] }
            },
            NullLogger.Instance,
            _ => null);

        var (exitCode, output) = await runtime.InstallDependenciesAsync(new DirectoryInfo(Path.GetTempPath()), CancellationToken.None);

        Assert.Equal(-1, exitCode);
        Assert.Collection(
            output.GetLines(),
            line =>
            {
                Assert.Equal("stderr", line.Stream);
                Assert.Equal("npm is not installed or not found in PATH. Please install Node.js and try again.", line.Line);
            });
    }

    [Fact]
    public async Task RunAsync_WhenNpxIsMissing_ReturnsNodeInstallMessage()
    {
        var runtime = new GuestRuntime(
            new RuntimeSpec
            {
                Language = KnownLanguageId.TypeScript,
                DisplayName = "TypeScript (Node.js)",
                CodeGenLanguage = "typescript",
                DetectionPatterns = ["apphost.ts"],
                Execute = new CommandSpec { Command = "npx", Args = ["tsx", "{appHostFile}"] }
            },
            NullLogger.Instance,
            _ => null);

        var appHostFile = new FileInfo(Path.Combine(Path.GetTempPath(), "apphost.ts"));
        var (exitCode, output) = await runtime.RunAsync(appHostFile, appHostFile.Directory!, new Dictionary<string, string>(), watchMode: false, CancellationToken.None);

        Assert.Equal(-1, exitCode);
        Assert.Collection(
            output.GetLines(),
            line =>
            {
                Assert.Equal("stderr", line.Stream);
                Assert.Equal("npx is not installed or not found in PATH. Please install Node.js and try again.", line.Line);
            });
    }
}
