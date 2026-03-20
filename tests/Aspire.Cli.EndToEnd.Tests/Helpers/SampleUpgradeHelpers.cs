// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Hex1b.Automation;
using Hex1b.Input;

namespace Aspire.Cli.EndToEnd.Tests.Helpers;

/// <summary>
/// Extension methods for <see cref="Hex1bTerminalAutomator"/> providing helpers for
/// sample upgrade E2E tests. These tests clone external repos (e.g., dotnet/aspire-samples),
/// run <c>aspire update</c> to upgrade them to the PR/CI build, and then run the apphost
/// to verify the sample works correctly.
/// </summary>
internal static class SampleUpgradeHelpers
{
    private const string DefaultSamplesRepoUrl = "https://github.com/dotnet/aspire-samples.git";
    private const string DefaultSamplesBranch = "main";

    /// <summary>
    /// Creates a NuGet.config in the current directory that includes the PR hive packages
    /// as a package source. This is needed because <c>aspire update --channel</c> uses a
    /// temporary NuGet config for the search phase but the apply phase (<c>dotnet add package</c>)
    /// needs the PR hive source in the project's NuGet config to resolve PR-versioned packages.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="channel">The PR channel name (e.g., <c>pr-15421</c>).</param>
    internal static async Task SetupPrHiveNuGetConfigAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string channel)
    {
        // Write a NuGet.config that includes both the PR hive source and nuget.org.
        // Package source mapping ensures Aspire* packages come from the hive.
        var hivePath = $"/root/.aspire/hives/{channel}/packages";
        var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <add key=""{channel}"" value=""{hivePath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key=""{channel}"">
      <package pattern=""Aspire*"" />
    </packageSource>
    <packageSource key=""nuget.org"">
      <package pattern=""*"" />
    </packageSource>
  </packageSourceMapping>
</configuration>";

        // Use heredoc to write the file
        await auto.TypeAsync($"cat > NuGet.config << 'NUGETEOF'\n{nugetConfig}\nNUGETEOF");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Clones a Git repository inside the container.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="repoUrl">The Git repository URL. Defaults to dotnet/aspire-samples.</param>
    /// <param name="branch">The branch to clone. Defaults to <c>main</c>.</param>
    /// <param name="depth">The clone depth. Defaults to 1 for shallow clone.</param>
    /// <param name="timeout">Timeout for the clone operation. Defaults to 120 seconds.</param>
    internal static async Task CloneSampleRepoAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string repoUrl = DefaultSamplesRepoUrl,
        string branch = DefaultSamplesBranch,
        int depth = 1,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(120);

        await auto.TypeAsync($"git clone --depth {depth} --single-branch --branch {branch} {repoUrl}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, effectiveTimeout);
    }

    /// <summary>
    /// Runs <c>aspire update</c> on a cloned sample, handling interactive prompts.
    /// Navigates to the sample directory, runs the update, and handles channel selection
    /// and CLI update prompts.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="samplePath">The relative path to the sample directory from the current working directory (e.g., <c>aspire-samples/samples/aspire-with-node</c>).</param>
    /// <param name="channel">Optional channel name to pass via <c>--channel</c>. When set, bypasses the interactive channel selection prompt and ensures the specified channel's packages are used.</param>
    /// <param name="timeout">Timeout for the update operation. Defaults to 180 seconds.</param>
    internal static async Task AspireUpdateInSampleAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string samplePath,
        string? channel = null,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(180);

        // Navigate to the sample directory
        await auto.TypeAsync($"cd {samplePath}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Run aspire update. When a channel is specified (e.g., "pr-15421"), pass it
        // explicitly via --channel to ensure PR hive packages are used for the upgrade
        // instead of the default nuget.org stable versions.
        var command = channel is not null
            ? $"aspire update --channel {channel}"
            : "aspire update";
        await auto.TypeAsync(command);
        await auto.EnterAsync();

        // Wait for completion. Handle interactive prompts along the way:
        // When using an explicit channel (e.g., PR hive), additional NuGet config prompts appear:
        // 1. "Which directory for NuGet.config file?" — accept default (Enter)
        // 2. "Apply these changes to NuGet.config?" — accept (Enter, default is yes)
        // Then for all modes:
        // 3. Channel selection prompt (if hives exist and no --channel) — select default (Enter)
        // 4. "Perform updates?" confirmation — accept (Enter, default is yes)
        // 5. CLI update prompt (after package update) — decline (type 'n')
        var expectedCounter = counter.Value;
        var channelPromptHandled = false;
        var nugetConfigDirPromptHandled = false;
        var nugetConfigApplyPromptHandled = false;
        var performUpdatesPromptHandled = false;
        var cliUpdatePromptHandled = false;

        await auto.WaitUntilAsync(snapshot =>
        {
            // Check if the command completed (success or error)
            var successSearcher = new CellPatternSearcher()
                .FindPattern(expectedCounter.ToString())
                .RightText(" OK] $ ");
            if (successSearcher.Search(snapshot).Count > 0)
            {
                return true;
            }

            var errorSearcher = new CellPatternSearcher()
                .FindPattern(expectedCounter.ToString())
                .RightText(" ERR:");
            if (errorSearcher.Search(snapshot).Count > 0)
            {
                return true;
            }

            // Handle "Select a channel:" prompt — select the first option (Enter)
            if (!channelPromptHandled && snapshot.ContainsText("Select a channel:"))
            {
                channelPromptHandled = true;
                _ = Task.Run(async () =>
                {
                    await auto.WaitAsync(500);
                    await auto.EnterAsync();
                });
            }

            // Handle "Which directory for NuGet.config file?" prompt — accept default (Enter)
            if (!nugetConfigDirPromptHandled && snapshot.ContainsText("NuGet.config file?"))
            {
                nugetConfigDirPromptHandled = true;
                _ = Task.Run(async () =>
                {
                    await auto.WaitAsync(500);
                    await auto.EnterAsync();
                });
            }

            // Handle "Apply these changes to NuGet.config?" prompt — accept (Enter)
            if (!nugetConfigApplyPromptHandled && snapshot.ContainsText("Apply these changes to NuGet.config?"))
            {
                nugetConfigApplyPromptHandled = true;
                _ = Task.Run(async () =>
                {
                    await auto.WaitAsync(500);
                    await auto.EnterAsync();
                });
            }

            // Handle "Perform updates?" confirmation — accept default (Enter)
            if (!performUpdatesPromptHandled && snapshot.ContainsText("Perform updates?"))
            {
                performUpdatesPromptHandled = true;
                _ = Task.Run(async () =>
                {
                    await auto.WaitAsync(500);
                    await auto.EnterAsync();
                });
            }

            // Handle "Would you like to update it now?" CLI update prompt — decline
            if (!cliUpdatePromptHandled && snapshot.ContainsText("Would you like to update it now?"))
            {
                cliUpdatePromptHandled = true;
                _ = Task.Run(async () =>
                {
                    await auto.WaitAsync(500);
                    await auto.TypeAsync("n");
                    await auto.EnterAsync();
                });
            }

            return false;
        }, timeout: effectiveTimeout, description: "aspire update to complete");

        counter.Increment();
    }

    /// <summary>
    /// Runs <c>aspire run</c> on a sample and waits for the apphost to start.
    /// Returns when the "Press CTRL+C to stop the apphost and exit." message is displayed.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="appHostRelativePath">Optional relative path to the AppHost csproj file. If specified, passed as <c>--apphost</c>.</param>
    /// <param name="startTimeout">Timeout for the apphost to start. Defaults to 5 minutes.</param>
    internal static async Task AspireRunSampleAsync(
        this Hex1bTerminalAutomator auto,
        string? appHostRelativePath = null,
        TimeSpan? startTimeout = null)
    {
        var effectiveTimeout = startTimeout ?? TimeSpan.FromMinutes(5);

        var command = appHostRelativePath is not null
            ? $"aspire run --apphost {appHostRelativePath}"
            : "aspire run";

        await auto.TypeAsync(command);
        await auto.EnterAsync();

        // Wait for the apphost to start successfully
        await auto.WaitUntilAsync(s =>
        {
            // Fail fast if apphost selection prompt appears (multiple apphosts detected)
            if (s.ContainsText("Select an apphost to use:"))
            {
                throw new InvalidOperationException(
                    "Unexpected apphost selection prompt detected! " +
                    "This indicates multiple apphosts were incorrectly detected in the sample.");
            }

            return s.ContainsText("Press CTRL+C to stop the apphost and exit.");
        }, timeout: effectiveTimeout, description: "aspire run to start (Press CTRL+C message)");
    }

    /// <summary>
    /// Stops a running <c>aspire run</c> instance by sending Ctrl+C.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="timeout">Timeout for the stop operation. Defaults to 60 seconds.</param>
    internal static async Task StopAspireRunAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(60);

        await auto.Ctrl().KeyAsync(Hex1bKey.C);
        await auto.WaitForSuccessPromptAsync(counter, effectiveTimeout);
    }

    /// <summary>
    /// Verifies that a sample's AppHost csproj was actually upgraded by checking that it
    /// no longer contains the original version string. This ensures <c>aspire update</c>
    /// actually modified the project file.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="csprojRelativePath">Relative path to the AppHost csproj from the sample directory.</param>
    /// <param name="originalVersion">The original Aspire version the sample was pinned to (e.g., <c>13.1.0</c>).</param>
    internal static async Task VerifySampleWasUpgradedAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string csprojRelativePath,
        string originalVersion)
    {
        // Check that the original version is no longer in the csproj
        await auto.TypeAsync($"grep -c '{originalVersion}' {csprojRelativePath} && echo 'UPGRADE_VERIFY_FAIL: still contains {originalVersion}' || echo 'UPGRADE_VERIFY_OK: no longer contains {originalVersion}'");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("UPGRADE_VERIFY_OK", timeout: TimeSpan.FromSeconds(10));
        await auto.WaitForAnyPromptAsync(counter);

        // Also print the current csproj for the recording so we can see what it was updated to
        await auto.TypeAsync($"cat {csprojRelativePath}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Verifies an HTTP endpoint is reachable from inside the container using <c>curl</c>.
    /// </summary>
    /// <param name="auto">The terminal automator.</param>
    /// <param name="counter">The sequence counter for prompt tracking.</param>
    /// <param name="url">The URL to check.</param>
    /// <param name="expectedStatusCode">The expected HTTP status code. Defaults to 200.</param>
    /// <param name="timeout">Timeout for the HTTP request. Defaults to 30 seconds.</param>
    internal static async Task VerifyHttpEndpointAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string url,
        int expectedStatusCode = 200,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var marker = $"endpoint-http-{expectedStatusCode}";

        await auto.TypeAsync(
            $"curl -ksSL -o /dev/null -w 'endpoint-http-%{{http_code}}' \"{url}\" " +
            "|| echo 'endpoint-http-failed'");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync(marker, timeout: effectiveTimeout);
        await auto.WaitForSuccessPromptAsync(counter);
    }
}
