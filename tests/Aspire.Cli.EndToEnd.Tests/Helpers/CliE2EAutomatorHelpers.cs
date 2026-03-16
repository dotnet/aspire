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
    /// </summary>
    internal static async Task InstallAspireCliInDockerAsync(
        this Hex1bTerminalAutomator auto,
        CliE2ETestHelpers.DockerInstallMode installMode,
        SequenceCounter counter)
    {
        switch (installMode)
        {
            case CliE2ETestHelpers.DockerInstallMode.SourceBuild:
                await auto.TypeAsync("mkdir -p ~/.aspire/bin && cp /opt/aspire-cli/aspire ~/.aspire/bin/aspire && chmod +x ~/.aspire/bin/aspire");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromSeconds(30));
                await auto.TypeAsync("export PATH=~/.aspire/bin:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
                break;

            case CliE2ETestHelpers.DockerInstallMode.GaRelease:
                await auto.TypeAsync("/opt/aspire-scripts/get-aspire-cli.sh");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(120));
                await auto.TypeAsync("export PATH=~/.aspire/bin:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
                break;

            case CliE2ETestHelpers.DockerInstallMode.PullRequest:
                var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
                await auto.TypeAsync($"/opt/aspire-scripts/get-aspire-cli-pr.sh {prNumber}");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
                await auto.TypeAsync("export PATH=~/.aspire/bin:~/.aspire:$PATH");
                await auto.EnterAsync();
                await auto.WaitForSuccessPromptAsync(counter);
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
    /// Installs the Aspire CLI from PR build artifacts in a non-Docker environment.
    /// </summary>
    internal static async Task InstallAspireCliFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        int prNumber,
        SequenceCounter counter)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Configures the PATH and environment variables for the Aspire CLI in a non-Docker environment.
    /// </summary>
    internal static async Task SourceAspireCliEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("export PATH=~/.aspire/bin:$PATH ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Verifies the installed Aspire CLI version matches the expected commit SHA.
    /// </summary>
    internal static async Task VerifyAspireCliVersionAsync(
        this Hex1bTerminalAutomator auto,
        string commitSha,
        SequenceCounter counter)
    {
        if (commitSha.Length != 40)
        {
            throw new ArgumentException($"Commit SHA must be exactly 40 characters, got {commitSha.Length}: '{commitSha}'", nameof(commitSha));
        }

        var shortCommitSha = commitSha[..8];
        var expectedVersionSuffix = $"g{shortCommitSha}";
        await auto.TypeAsync("aspire --version");
        await auto.EnterAsync();
        await auto.WaitUntilTextAsync(expectedVersionSuffix, timeout: TimeSpan.FromSeconds(10));
        await auto.WaitForSuccessPromptAsync(counter);
    }

    /// <summary>
    /// Installs the Aspire CLI and bundle from PR build artifacts, using the PR head SHA to fetch the install script.
    /// </summary>
    internal static async Task InstallAspireBundleFromPullRequestAsync(
        this Hex1bTerminalAutomator auto,
        int prNumber,
        SequenceCounter counter)
    {
        var command = $"ref=$(gh api repos/dotnet/aspire/pulls/{prNumber} --jq '.head.sha') && curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/$ref/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Configures the PATH and environment variables for the Aspire CLI bundle in a non-Docker environment.
    /// Unlike <see cref="SourceAspireCliEnvironmentAsync"/>, this includes <c>~/.aspire</c> in PATH for bundle tools.
    /// </summary>
    internal static async Task SourceAspireBundleEnvironmentAsync(
        this Hex1bTerminalAutomator auto,
        SequenceCounter counter)
    {
        await auto.TypeAsync("export PATH=~/.aspire/bin:~/.aspire:$PATH ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false");
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
    /// Installs a specific GA version of the Aspire CLI using the install script.
    /// </summary>
    internal static async Task InstallAspireCliVersionAsync(
        this Hex1bTerminalAutomator auto,
        string version,
        SequenceCounter counter)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli.sh | bash -s -- --version \"{version}\"";
        await auto.TypeAsync(command);
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptFailFastAsync(counter, timeout: TimeSpan.FromSeconds(300));
    }
}
