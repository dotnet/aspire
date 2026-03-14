// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for Aspire CLI start and stop commands (background/detached mode).
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class StartStopTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CreateStartAndStopAspireProject()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create a new project using aspire new
        await auto.AspireNewAsync("AspireStarterApp", counter);

        // Navigate to the AppHost directory
        await auto.TypeAsync("cd AspireStarterApp/AspireStarterApp.AppHost");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Start the AppHost in the background using aspire start
        await auto.TypeAsync("aspire start");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("AppHost started successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(3),
            description: "AppHost started successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Stop the AppHost using aspire stop
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("AppHost stopped successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(1),
            description: "AppHost stopped successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task StopWithNoRunningAppHostExitsSuccessfully()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Run aspire stop with no running AppHost - should exit with code 0
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("No running AppHost found").Search(s).Count > 0,
            timeout: TimeSpan.FromSeconds(30),
            description: "No running AppHost found message");
        await auto.WaitForSuccessPromptAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task AddPackageWhileAppHostRunningDetached()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create a new project using aspire new
        await auto.AspireNewAsync("AspireAddTestApp", counter);

        // Navigate to the AppHost directory
        await auto.TypeAsync("cd AspireAddTestApp/AspireAddTestApp.AppHost");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Start the AppHost in detached mode (locks the project file)
        await auto.TypeAsync("aspire start");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("AppHost started successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(3),
            description: "AppHost started successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Add a package while the AppHost is running - this should auto-stop the
        // running instance before modifying the project, then succeed.
        // --non-interactive skips the version selection prompt.
        await auto.TypeAsync("aspire add mongodb --non-interactive");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("was added successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(3),
            description: "package added successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Clean up: stop if still running (the add command may have stopped it)
        // aspire stop may return a non-zero exit code if no instances are found
        // (already stopped by aspire add), so wait for known output patterns.
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            var noRunning = new CellPatternSearcher().Find("No running AppHost found").Search(s).Count > 0;
            var stopped = new CellPatternSearcher().Find("AppHost stopped successfully.").Search(s).Count > 0;
            return noRunning || stopped;
        }, timeout: TimeSpan.FromMinutes(1), description: "AppHost stopped or no running AppHost");
        await auto.WaitForAnyPromptAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }

    [Fact]
    public async Task AddPackageInteractiveWhileAppHostRunningDetached()
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var installMode = CliE2ETestHelpers.DetectDockerInstallMode(repoRoot);

        var workspace = TemporaryWorkspace.Create(output);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, installMode, output, mountDockerSocket: true, workspace: workspace);

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

        // Prepare Docker environment (prompt counting, umask, env vars)
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);

        // Install the Aspire CLI
        await auto.InstallAspireCliInDockerAsync(installMode, counter);

        // Create a new project using aspire new
        await auto.AspireNewAsync("AspireAddInteractiveApp", counter);

        // Navigate to the AppHost directory
        await auto.TypeAsync("cd AspireAddInteractiveApp/AspireAddInteractiveApp.AppHost");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Start the AppHost in detached mode (locks the project file)
        await auto.TypeAsync("aspire start");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("AppHost started successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(3),
            description: "AppHost started successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Run aspire add interactively (no integration argument) while AppHost is running.
        // This exercises the interactive package selection flow and verifies the
        // running instance is auto-stopped before modifying the project.
        await auto.TypeAsync("aspire add");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("Select an integration to add:").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(1),
            description: "integration selection prompt");
        await auto.TypeAsync("mongodb"); // type to filter the list
        await auto.EnterAsync(); // select the filtered result
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("Select a version of").Search(s).Count > 0,
            timeout: TimeSpan.FromSeconds(30),
            description: "version selection prompt");
        await auto.EnterAsync(); // Accept the default version
        await auto.WaitUntilAsync(
            s => new CellPatternSearcher().Find("was added successfully.").Search(s).Count > 0,
            timeout: TimeSpan.FromMinutes(2),
            description: "package added successfully");
        await auto.WaitForSuccessPromptAsync(counter);

        // Clean up: stop if still running
        // aspire stop may return a non-zero exit code if no instances are found
        // (already stopped by aspire add), so wait for known output patterns.
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitUntilAsync(s =>
        {
            var noRunning = new CellPatternSearcher().Find("No running AppHost found").Search(s).Count > 0;
            var stopped = new CellPatternSearcher().Find("AppHost stopped successfully.").Search(s).Count > 0;
            return noRunning || stopped;
        }, timeout: TimeSpan.FromMinutes(1), description: "AppHost stopped or no running AppHost");
        await auto.WaitForAnyPromptAsync(counter);

        // Exit the shell
        await auto.TypeAsync("exit");
        await auto.EnterAsync();

        await pendingRun;
    }
}
