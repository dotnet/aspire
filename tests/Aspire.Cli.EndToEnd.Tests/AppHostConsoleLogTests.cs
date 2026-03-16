// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Hex1b.Input;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests ensuring apphost console output is visible during non-detached <c>aspire run</c>.
/// </summary>
public sealed class AppHostConsoleLogTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Run_ShowsDotNetAppHostConsoleOutput()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);
        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        await auto.AspireInitAsync(counter);

        var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs");
        InsertAfter(appHostPath, "var builder = DistributedApplication.CreateBuilder(args);", """Console.WriteLine("Hello from dotnet apphost");""");

        await auto.TypeAsync("aspire run --apphost apphost.cs");
        await auto.EnterAsync();
        await WaitForVisibleAppHostOutputAsync(auto, "Hello from dotnet apphost", TimeSpan.FromMinutes(3));

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task Run_ShowsTypeScriptAppHostConsoleOutput()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);
        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, workspace: workspace);
        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        await auto.TypeAsync("aspire init --language typescript --non-interactive");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("Created apphost.ts", timeout: TimeSpan.FromMinutes(2));
        await auto.WaitForSuccessPromptAsync(counter);

        var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.ts");
        InsertAfter(appHostPath, "const builder = await createBuilder();", "console.log('Hello from typescript apphost');");

        await auto.TypeAsync("aspire run --apphost apphost.ts");
        await auto.EnterAsync();
        await WaitForVisibleAppHostOutputAsync(auto, "Hello from typescript apphost", TimeSpan.FromMinutes(3));

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    private static void InsertAfter(string filePath, string marker, string lineToInsert)
    {
        var content = File.ReadAllText(filePath);
        Assert.Contains(marker, content);

        var updatedContent = content.Replace(
            marker,
            $"{marker}{Environment.NewLine}{Environment.NewLine}{lineToInsert}",
            StringComparison.Ordinal);

        File.WriteAllText(filePath, updatedContent);
    }

    private static async Task WaitForVisibleAppHostOutputAsync(Hex1bTerminalAutomator auto, string expectedOutput, TimeSpan timeout)
    {
        await auto.WaitUntilAsync(snapshot =>
        {
            if (snapshot.ContainsText("Select an apphost to use:"))
            {
                throw new InvalidOperationException("Unexpected apphost selection prompt detected.");
            }

            return snapshot.ContainsText(expectedOutput);
        }, timeout: timeout, description: $"waiting for apphost output '{expectedOutput}'");

        await auto.WaitUntilTextAsync("Press CTRL+C to stop the apphost and exit.", timeout: timeout);
    }
}
