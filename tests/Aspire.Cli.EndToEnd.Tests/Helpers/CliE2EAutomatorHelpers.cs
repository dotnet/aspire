// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;

namespace Aspire.Cli.EndToEnd.Tests.Helpers;

/// <summary>
/// Extension methods for <see cref="Hex1bTerminalAutomator"/> providing Docker E2E test helpers.
/// These parallel the <see cref="Hex1b.Automation.Hex1bTerminalInputSequenceBuilder"/>-based methods in <see cref="CliE2ETestHelpers"/>.
/// </summary>
internal static class CliE2EAutomatorHelpers
{
    /// <summary>
    /// Prepares the Docker environment by setting up prompt counting, umask, and environment variables.
    /// </summary>
    internal static async Task PrepareDockerEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        TemporaryWorkspace? workspace = null)
    {
        // Wait for container to be ready (root prompt)
        await auto.WaitUntilTextAsync("# ", timeout: TimeSpan.FromSeconds(60));

        await auto.WaitAsync(500);

        // Set up the prompt counting mechanism
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";
        await auto.TypeAsync(promptSetup);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Set permissive umask
        await auto.TypeAsync("umask 000");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // Set environment variables
        await auto.TypeAsync("export ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        if (workspace is not null)
        {
            await auto.TypeAsync($"cd /workspace/{workspace.WorkspaceRoot.Name}");
            await auto.EnterAsync();
            await auto.WaitForSuccessPromptAsync(counter);
        }
    }

    /// <summary>
    /// Installs the Aspire CLI inside a Docker container based on the detected install mode.
    /// Installs to a temporary directory rather than the default ~/.aspire.
    /// <para>Env var overrides:</para>
    /// <list type="bullet">
    /// <item><c>ASPIRE_CLI_USE_SYSTEM=true</c> — skip install, use system aspire</item>
    /// <item><c>ASPIRE_CLI_INSTALL_SCRIPT</c> — <c>get-aspire-cli</c> or <c>get-aspire-cli-pr</c></item>
    /// <item><c>ASPIRE_CLI_INSTALL_ARGS</c> — extra args for the script (e.g. <c>-q dev</c> or <c>15414</c>)</item>
    /// </list>
    /// </summary>
    internal static async Task InstallAspireCliInDockerAsync(
        this Hex1bTerminalAutomator auto,
        CliE2ETestHelpers.DockerInstallMode installMode,
        SequenceCounter counter)
    {
        if (UseSystemCli)
        {
            await auto.EchoAspirePathAsync(counter);
            return;
        }

        var scriptOverride = Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_SCRIPT");
        if (!string.IsNullOrEmpty(scriptOverride))
        {
            const string installPath = "/tmp/aspire-install";
            await auto.RemoveDefaultAspireBinAsync(counter);
            await auto.InstallFromScriptInDockerAsync(scriptOverride, installPath, counter);
            await auto.EnableStagingChannelIfNeededAsync(counter);
            return;
        }

        const string defaultInstallPath = "/tmp/aspire-install";

        // Remove any CLI binary at the default location so only the temp install is used
        await auto.RemoveDefaultAspireBinAsync(counter);

        switch (installMode)
        {
            case CliE2ETestHelpers.DockerInstallMode.SourceBuild:
                await auto.TypeAsync($"mkdir -p {defaultInstallPath}/bin && cp /opt/aspire-cli/aspire {defaultInstallPath}/bin/aspire && chmod +x {defaultInstallPath}/bin/aspire");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromSeconds(30));
                await auto.TypeAsync($"export PATH={defaultInstallPath}/bin:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
                await auto.EchoAspirePathAsync(counter);
                break;

            case CliE2ETestHelpers.DockerInstallMode.GaRelease:
                await auto.TypeAsync($"/opt/aspire-scripts/get-aspire-cli.sh --install-path {defaultInstallPath}/bin");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(120));
                await auto.TypeAsync($"export PATH={defaultInstallPath}/bin:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
                await auto.EchoAspirePathAsync(counter);
                break;

            case CliE2ETestHelpers.DockerInstallMode.PullRequest:
                var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
                await auto.TypeAsync($"/opt/aspire-scripts/get-aspire-cli-pr.sh {prNumber} --install-path {defaultInstallPath}");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
                await auto.TypeAsync($"export PATH={defaultInstallPath}/bin:{defaultInstallPath}:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
                await auto.EchoAspirePathAsync(counter);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(installMode));
        }
    }

    /// <summary>
    /// Prepares a non-Docker terminal environment with prompt counting and workspace navigation.
    /// Used by tests that run with <see cref="CliE2ETestHelpers.CreateTestTerminal"/> (bare bash, no Docker).
    /// </summary>
    internal static async Task PrepareEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        TemporaryWorkspace workspace,
        SequenceCounter counter)
    {
        var waitingForInputPattern = new CellPatternSearcher()
            .Find("b").RightUntil("$").Right(' ').Right(' ');

        await auto.WaitUntilAsync(
            s => waitingForInputPattern.Search(s).Count > 0,
            timeout: TimeSpan.FromSeconds(10),
            description: "initial bash prompt");
        await auto.WaitAsync(500);

        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";
        await auto.TypeAsync(promptSetup);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync($"cd {workspace.WorkspaceRoot.FullName}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// High-level setup that installs the Aspire CLI and configures the environment.
    /// Supports env var overrides (see <see cref="InstallAspireCliInDockerAsync"/>).
    /// No-op when not in CI and no overrides are set.
    /// </summary>
    internal static async Task SetupAspireCliFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        if (UseSystemCli)
        {
            await auto.EchoAspirePathAsync(counter);
            return;
        }

        var scriptOverride = Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_SCRIPT");
        if (!string.IsNullOrEmpty(scriptOverride))
        {
            var installPath = CreateTemporaryInstallPath();
            await auto.RemoveDefaultAspireBinAsync(counter);
            await auto.InstallFromScriptAsync(scriptOverride, installPath, counter);
            await auto.SourceAspireCliEnvironmentAsync(counter, installPath);
            await auto.EnableStagingChannelIfNeededAsync(counter);
            return;
        }

        if (!CliE2ETestHelpers.IsRunningInCI)
        {
            return;
        }

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var prInstallPath = CreateTemporaryInstallPath();

        await auto.RemoveDefaultAspireBinAsync(counter);
        await auto.InstallAspireCliFromPullRequestAsync(prNumber, counter, prInstallPath);
        await auto.SourceAspireCliEnvironmentAsync(counter, prInstallPath);
        await auto.VerifyAspireCliVersionAsync(commitSha, counter);
    }

    /// <summary>
    /// High-level setup that installs the Aspire CLI bundle and configures the environment.
    /// Supports env var overrides (see <see cref="InstallAspireCliInDockerAsync"/>).
    /// No-op when not in CI and no overrides are set.
    /// </summary>
    internal static async Task SetupAspireBundleFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        if (UseSystemCli)
        {
            await auto.EchoAspirePathAsync(counter);
            return;
        }

        var scriptOverride = Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_SCRIPT");
        if (!string.IsNullOrEmpty(scriptOverride))
        {
            var installPath = CreateTemporaryInstallPath();
            await auto.RemoveDefaultAspireBinAsync(counter);
            await auto.InstallFromScriptAsync(scriptOverride, installPath, counter);
            await auto.SourceAspireBundleEnvironmentAsync(counter, installPath);
            await auto.EnableStagingChannelIfNeededAsync(counter);
            return;
        }

        if (!CliE2ETestHelpers.IsRunningInCI)
        {
            return;
        }

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var prInstallPath = CreateTemporaryInstallPath();

        await auto.RemoveDefaultAspireBinAsync(counter);
        await auto.InstallAspireBundleFromPullRequestAsync(prNumber, counter, prInstallPath);
        await auto.SourceAspireBundleEnvironmentAsync(counter, prInstallPath);
        await auto.VerifyAspireCliVersionAsync(commitSha, counter);
    }

    /// <summary>
    /// Creates a unique temporary directory path for CLI installation.
    /// </summary>
    private static string CreateTemporaryInstallPath()
    {
        return Path.Combine(Path.GetTempPath(), $"aspire-e2e-{Guid.NewGuid():N}");
    }

    /// <summary>
    /// Gets whether the tests should use the system-installed <c>aspire</c> already in PATH,
    /// skipping all installation steps. Enabled by setting <c>ASPIRE_CLI_USE_SYSTEM=true</c>.
    /// </summary>
    private static bool UseSystemCli =>
        string.Equals(Environment.GetEnvironmentVariable("ASPIRE_CLI_USE_SYSTEM"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether staging quality was requested via <c>ASPIRE_CLI_INSTALL_ARGS</c>.
    /// </summary>
    private static bool IsStagingQuality =>
        (Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_ARGS") ?? "").Contains("-q staging", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Enables the staging channel in aspire config if staging quality was requested.
    /// Sets both the feature flag and the default channel so that <c>aspire new</c>
    /// generates a NuGet.config pointing to the staging feed.
    /// </summary>
    private static async Task EnableStagingChannelIfNeededAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        if (!IsStagingQuality)
        {
            return;
        }

        await auto.TypeAsync("aspire config set -g features.stagingChannelEnabled true");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromSeconds(30));

        await auto.TypeAsync("aspire config set -g channel staging");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Sets the staging channel in the local <c>aspire.config.json</c> so that <c>aspire add</c>
    /// can find unreleased packages in TypeScript/polyglot apphosts (which don't use NuGet.config).
    /// Call this after <c>aspire init</c> for TypeScript projects when staging quality is in use.
    /// </summary>
    internal static async Task SetLocalStagingChannelIfNeededAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        if (!IsStagingQuality)
        {
            return;
        }

        await auto.TypeAsync("aspire config set channel staging");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Waits for either the version selection prompt or the success prompt after <c>aspire add</c>.
    /// When multiple channels exist (e.g. PR hives), the version selector appears and is accepted.
    /// When only the implicit channel exists (bare CLI install), the add completes directly.
    /// </summary>
    internal static async Task AcceptVersionSelectionIfShownAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(180);

        var waitingForVersionSelection = new CellPatternSearcher()
            .Find("Select a version of");
        var versionSelectionShown = false;

        var successPromptSearcher = new CellPatternSearcher()
            .FindPattern(counter.Value.ToString())
            .RightText(" OK] $ ");

        await auto.WaitUntilAsync(s =>
        {
            if (waitingForVersionSelection.Search(s).Count > 0)
            {
                versionSelectionShown = true;
                return true;
            }

            return successPromptSearcher.Search(s).Count > 0;
        }, timeout: timeout.Value, description: "version selection prompt or success prompt");

        if (versionSelectionShown)
        {
            await auto.EnterAsync(); // Accept the default version
        }

        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Builds the install command for a script override.
    /// Uses <c>ASPIRE_CLI_INSTALL_SCRIPT</c> to pick the script name and
    /// <c>ASPIRE_CLI_INSTALL_ARGS</c> for any extra arguments.
    /// </summary>
    private static string BuildScriptCommand(string scriptName, string installPathArg)
    {
        var extraArgs = Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_ARGS") ?? "";
        if (!string.IsNullOrEmpty(extraArgs))
        {
            extraArgs = " " + extraArgs;
        }

        return $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/{scriptName}.sh | bash -s --{extraArgs} --install-path {installPathArg}";
    }

    /// <summary>
    /// Installs the CLI using a script override inside a Docker container.
    /// The script is fetched from <c>/opt/aspire-scripts/</c> inside the container.
    /// </summary>
    private static async Task InstallFromScriptInDockerAsync(
        this Hex1bTerminalAutomator auto,
        string scriptName,
        string installPath,
        SequenceCounter counter)
    {
        var extraArgs = Environment.GetEnvironmentVariable("ASPIRE_CLI_INSTALL_ARGS") ?? "";
        if (!string.IsNullOrEmpty(extraArgs))
        {
            extraArgs = " " + extraArgs;
        }

        // get-aspire-cli installs directly to --install-path; get-aspire-cli-pr puts bin/ under it
        var installPathArg = scriptName == "get-aspire-cli" ? $"{installPath}/bin" : installPath;
        await auto.TypeAsync($"/opt/aspire-scripts/{scriptName}.sh{extraArgs} --install-path {installPathArg}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));

        await auto.TypeAsync($"export PATH={installPath}/bin:{installPath}:$PATH");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.EchoAspirePathAsync(counter);
    }

    /// <summary>
    /// Installs the CLI using a script override in a non-Docker environment (downloads from GitHub).
    /// </summary>
    private static async Task InstallFromScriptAsync(
        this Hex1bTerminalAutomator auto,
        string scriptName,
        string installPath,
        SequenceCounter counter)
    {
        // get-aspire-cli installs directly to --install-path; get-aspire-cli-pr puts bin/ under it
        var installPathArg = scriptName == "get-aspire-cli" ? $"{installPath}/bin" : installPath;
        var command = BuildScriptCommand(scriptName, installPathArg);
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Removes the default <c>~/.aspire/bin</c> directory so a stale install can't interfere.
    /// </summary>
    private static async Task RemoveDefaultAspireBinAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("rm -rf ~/.aspire/bin");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Installs the Aspire CLI from PR build artifacts in a non-Docker environment.
    /// </summary>
    private static async Task InstallAspireCliFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        int prNumber,
        SequenceCounter counter,
        string installPath)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber} --install-path {installPath}";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Configures the PATH and environment variables for the Aspire CLI in a non-Docker environment.
    /// </summary>
    private static async Task SourceAspireCliEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string installPath)
    {
        await auto.TypeAsync($"export PATH={installPath}/bin:$PATH ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.EchoAspirePathAsync(counter);
    }

    /// <summary>
    /// Verifies the installed Aspire CLI version matches the expected version.
    /// </summary>
#pragma warning disable IDE0060 // commitSha is unused during stabilized builds — restore when merging back to main
    internal static async Task VerifyAspireCliVersionAsync(
        this Hex1bTerminalAutomator auto,
        string commitSha,
        SequenceCounter counter)
#pragma warning restore IDE0060
    {
        await auto.TypeAsync("aspire --version");
        await auto.EnterAsync();

        // When the build is stabilized (StabilizePackageVersion=true), the CLI version
        // is just "13.2.0" with no commit SHA suffix. When not stabilized, it includes
        // the SHA (e.g., "13.2.0-preview.1.g<sha>"). In both cases, "13.2.0" is present.
        // TODO: This change should be reverted on the integration to the main branch.
        await auto.WaitUntilTextAsync("13.2.0", timeout: TimeSpan.FromSeconds(10));

        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Installs the Aspire CLI and bundle from PR build artifacts, using the PR head SHA to fetch the install script.
    /// </summary>
    private static async Task InstallAspireBundleFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        int prNumber,
        SequenceCounter counter,
        string installPath)
    {
        var command = $"ref=$(gh api repos/dotnet/aspire/pulls/{prNumber} --jq '.head.sha') && curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/$ref/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber} --install-path {installPath}";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Configures the PATH and environment variables for the Aspire CLI bundle in a non-Docker environment.
    /// </summary>
    private static async Task SourceAspireBundleEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        string installPath)
    {
        await auto.TypeAsync($"export PATH={installPath}/bin:{installPath}:$PATH ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.EchoAspirePathAsync(counter);
    }

    /// <summary>
    /// Echoes the resolved path of the <c>aspire</c> binary for diagnostic visibility.
    /// </summary>
    private static async Task EchoAspirePathAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("which aspire");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync("aspire --version");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Clears the terminal screen by running the <c>clear</c> command and waiting for the prompt.
    /// </summary>
    internal static async Task ClearScreenAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("clear");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Ensures polyglot support is enabled for tests.
    /// Polyglot support now defaults to enabled, so this is currently a no-op.
    /// </summary>
    internal static Task EnablePolyglotSupportAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        _ = auto;
        _ = counter;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Installs a specific GA version of the Aspire CLI using the install script to a temporary directory.
    /// Also sets up PATH so the installed CLI is available. Used inside Docker containers.
    /// </summary>
    internal static async Task InstallAspireCliVersionAsync(
        this Hex1bTerminalAutomator auto,
        string version,
        SequenceCounter counter)
    {
        if (UseSystemCli)
        {
            await auto.EchoAspirePathAsync(counter);
            return;
        }

        const string installPath = "/tmp/aspire-version-install";

        // Remove any CLI binary at the default location and clean up any previous version install
        await auto.TypeAsync($"rm -rf ~/.aspire/bin {installPath}");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        // get-aspire-cli.sh --install-path takes the direct bin directory
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli.sh | bash -s -- --version \"{version}\" --install-path {installPath}/bin";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, timeout: TimeSpan.FromSeconds(300));

        await auto.TypeAsync($"export PATH={installPath}/bin:$PATH");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.EchoAspirePathAsync(counter);
    }

    /// <summary>
    /// Starts an Aspire AppHost with <c>aspire start --format json</c>, extracts the dashboard URL,
    /// and verifies the dashboard is reachable. Caller is responsible for calling
    /// <see cref="AspireStopAsync"/> when done.
    /// On failure, dumps the latest CLI log file to the terminal output for debugging.
    /// </summary>
    internal static async Task AspireStartAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter,
        TimeSpan? startTimeout = null)
    {
        var effectiveTimeout = startTimeout ?? TimeSpan.FromMinutes(3);
        var jsonFile = "/tmp/aspire-start.json";
        var expectedCounter = counter.Value;

        // Start with JSON output
        await auto.TypeAsync($"aspire start --format json | tee {jsonFile}");
        await auto.EnterAsync();

        // Wait for the command to finish — check for success or error exit
        var succeeded = false;
        await auto.WaitUntilAsync(snapshot =>
        {
            var successSearcher = new CellPatternSearcher()
                .FindPattern(expectedCounter.ToString())
                .RightText(" OK] $ ");
            if (successSearcher.Search(snapshot).Count > 0)
            {
                succeeded = true;
                return true;
            }

            var errorSearcher = new CellPatternSearcher()
                .FindPattern(expectedCounter.ToString())
                .RightText(" ERR:");
            return errorSearcher.Search(snapshot).Count > 0;
        }, timeout: effectiveTimeout, description: $"aspire start to complete [{expectedCounter} OK/ERR]");

        counter.Increment();

        if (!succeeded)
        {
            // Dump logs for debugging then fail
            await auto.TypeAsync(
                "LOG=$(ls -t ~/.aspire/logs/cli_*.log 2>/dev/null | head -1); " +
                "echo '=== ASPIRE LOG ==='; " +
                "[ -n \"$LOG\" ] && tail -100 \"$LOG\"; " +
                "echo '=== END LOG ==='; " +
                $"cat {jsonFile}");
            await auto.EnterAsync();
            await auto.WaitForSuccessPromptAsync(counter);

            throw new InvalidOperationException("aspire start failed. Check terminal output for CLI logs.");
        }

        // Extract dashboard URL and verify it's reachable
        await auto.TypeAsync(
            $"DASHBOARD_URL=$(sed -n " +
            "'s/.*\"dashboardUrl\"[[:space:]]*:[[:space:]]*\"\\(https:\\/\\/localhost:[0-9]*\\).*/\\1/p' " +
            $"{jsonFile} | head -1)");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);

        await auto.TypeAsync(
            "curl -ksSL -o /dev/null -w 'dashboard-http-%{http_code}' \"$DASHBOARD_URL\" " +
            "|| echo 'dashboard-http-failed'");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync("dashboard-http-200", timeout: TimeSpan.FromSeconds(15));
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Stops a running Aspire AppHost with <c>aspire stop</c>.
    /// </summary>
    internal static async Task AspireStopAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("aspire stop");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }
}
