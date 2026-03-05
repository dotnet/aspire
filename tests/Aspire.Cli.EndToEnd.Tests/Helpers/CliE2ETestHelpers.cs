// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests.Helpers;

/// <summary>
/// Helper methods for creating and managing Hex1b terminal sessions for Aspire CLI testing.
/// </summary>
internal static class CliE2ETestHelpers
{
    /// <summary>
    /// Gets whether the tests are running in CI (GitHub Actions) vs locally.
    /// When running locally, some commands are replaced with echo stubs.
    /// </summary>
    internal static bool IsRunningInCI =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER")) &&
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_PR_HEAD_SHA"));

    /// <summary>
    /// Gets the PR number from the GITHUB_PR_NUMBER environment variable.
    /// When running locally (not in CI), returns a dummy value (0) for testing.
    /// </summary>
    /// <returns>The PR number, or 0 when running locally.</returns>
    internal static int GetRequiredPrNumber()
    {
        var prNumberStr = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER");

        if (string.IsNullOrEmpty(prNumberStr))
        {
            // Running locally - return dummy value
            return 0;
        }

        Assert.True(int.TryParse(prNumberStr, out var prNumber), $"GITHUB_PR_NUMBER must be a valid integer, got: {prNumberStr}");
        return prNumber;
    }

    /// <summary>
    /// Gets the commit SHA from the GITHUB_PR_HEAD_SHA environment variable.
    /// This is the actual PR head commit, not the merge commit (GITHUB_SHA).
    /// When running locally (not in CI), returns a dummy value for testing.
    /// </summary>
    /// <returns>The commit SHA, or a dummy value when running locally.</returns>
    internal static string GetRequiredCommitSha()
    {
        var commitSha = Environment.GetEnvironmentVariable("GITHUB_PR_HEAD_SHA");

        if (string.IsNullOrEmpty(commitSha))
        {
            // Running locally - return dummy value
            return "local0000";
        }

        return commitSha;
    }

    /// <summary>
    /// Gets the path for storing asciinema recordings that will be uploaded as CI artifacts.
    /// In CI, this returns a path under $GITHUB_WORKSPACE/testresults/recordings/.
    /// Locally, this returns a path under the system temp directory.
    /// </summary>
    /// <param name="testName">The name of the test (used as the recording filename).</param>
    /// <returns>The full path to the .cast recording file.</returns>
    internal static string GetTestResultsRecordingPath(string testName)
    {
        return Hex1bTestHelpers.GetTestResultsRecordingPath(testName, "aspire-cli-e2e");
    }

    /// <summary>
    /// Creates a headless Hex1b terminal configured for E2E testing with asciinema recording.
    /// Uses default dimensions of 160x48 unless overridden.
    /// </summary>
    /// <param name="testName">The test name used for the recording file path. Defaults to the calling method name.</param>
    /// <param name="width">The terminal width in columns. Defaults to 160.</param>
    /// <param name="height">The terminal height in rows. Defaults to 48.</param>
    /// <returns>A configured <see cref="Hex1bTerminal"/> instance. Caller is responsible for disposal.</returns>
    internal static Hex1bTerminal CreateTestTerminal(int width = 160, int height = 48, [CallerMemberName] string testName = "")
    {
        return Hex1bTestHelpers.CreateTestTerminal("aspire-cli-e2e", width, height, testName);
    }

    internal static Hex1bTerminalInputSequenceBuilder PrepareEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder, TemporaryWorkspace workspace, SequenceCounter counter)
    {
        var waitingForInputPattern = new CellPatternSearcher()
            .Find("b").RightUntil("$").Right(' ').Right(' ');

        builder.WaitUntil(s => waitingForInputPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Wait(500); // Small delay to ensure terminal is ready.

        if (OperatingSystem.IsWindows())
        {
            // PowerShell prompt setup
            const string promptSetup = "$global:CMDCOUNT=0; function prompt { $s=$?; $global:CMDCOUNT++; \"[$global:CMDCOUNT $(if($s){'OK'}else{\"ERR:$LASTEXITCODE\"})] PS> \" }";
            builder.Type(promptSetup).Enter();
        }
        else
        {
            // Bash prompt setup
            const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";
            builder.Type(promptSetup).Enter();
        }

        return builder.WaitForSuccessPrompt(counter)
            .Type($"cd {workspace.WorkspaceRoot.FullName}").Enter()
            .WaitForSuccessPrompt(counter);
    }

    internal static Hex1bTerminalInputSequenceBuilder InstallAspireCliFromPullRequest(
        this Hex1bTerminalInputSequenceBuilder builder,
        int prNumber,
        SequenceCounter counter)
    {
        string command;
        if (OperatingSystem.IsWindows())
        {
            // PowerShell installation command
            command = $"iex \"& {{ $(irm https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.ps1) }} {prNumber}\"";
        }
        else
        {
            // Bash installation command
            command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";
        }

        return builder
            .Type(command)
            .Enter()
            .WaitForSuccessPromptFailFast(counter, TimeSpan.FromSeconds(300));
    }

    internal static Hex1bTerminalInputSequenceBuilder SourceAspireCliEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter
        )
    {
        if (OperatingSystem.IsWindows())
        {
            // On Windows, the PowerShell installer already updates the current session's PATH
            // But we still need to set ASPIRE_PLAYGROUND for interactive mode and .NET CLI vars
            return builder
                .Type("$env:ASPIRE_PLAYGROUND='true'; $env:DOTNET_CLI_TELEMETRY_OPTOUT='true'; $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='true'; $env:DOTNET_GENERATE_ASPNET_CERTIFICATE='false'")
                .Enter()
                .WaitForSuccessPrompt(counter);
        }

        // The installer adds aspire to ~/.aspire/bin
        // We need to add it to PATH and set environment variables:
        // - ASPIRE_PLAYGROUND=true enables interactive mode
        // - TERM=xterm enables clear command and other terminal features
        // - .NET CLI vars suppress telemetry and first-time experience which can cause hangs
        return builder
            .Type("export PATH=~/.aspire/bin:$PATH ASPIRE_PLAYGROUND=true TERM=xterm DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Verifies that the installed Aspire CLI version matches the expected commit SHA.
    /// Runs 'aspire --version' and checks that the output contains the expected version suffix.
    /// PR builds have version format: {version}-pr.{prNumber}.g{shortCommitSha}
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="commitSha">The full 40-character commit SHA to verify against.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when commitSha is not exactly 40 characters.</exception>
    internal static Hex1bTerminalInputSequenceBuilder VerifyAspireCliVersion(
        this Hex1bTerminalInputSequenceBuilder builder,
        string commitSha,
        SequenceCounter counter)
    {
        // Git SHA-1 hashes are exactly 40 hexadecimal characters
        if (commitSha.Length != 40)
        {
            throw new ArgumentException($"Commit SHA must be exactly 40 characters, got {commitSha.Length}: '{commitSha}'", nameof(commitSha));
        }

        // PR builds use the format: {version}-pr.{prNumber}.g{shortCommitSha}
        // The short commit SHA is 8 characters, prefixed with 'g' (git convention)
        var shortCommitSha = commitSha[..8];
        var expectedVersionSuffix = $"g{shortCommitSha}";

        var versionPattern = new CellPatternSearcher()
            .Find(expectedVersionSuffix);

        return builder
            .Type("aspire --version")
            .Enter()
            .WaitUntil(s => versionPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Ensures polyglot support is enabled for tests.
    /// Polyglot support now defaults to enabled, so this is currently a no-op.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder EnablePolyglotSupport(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        _ = counter;
        return builder;
    }

    /// <summary>
    /// Clears SSL_CERT_DIR environment variable to simulate partial trust scenario on Linux.
    /// When SSL_CERT_DIR is not set, dev certificates are only partially trusted because
    /// OpenSSL doesn't know to look in ~/.aspnet/dev-certs/trust for the certificate.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder ClearSslCertDir(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        return builder
            .Type("unset SSL_CERT_DIR")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Configures SSL_CERT_DIR environment variable to include the dev-certs trust path.
    /// This enables full trust for dev certificates on Linux by telling OpenSSL where to
    /// find the trusted certificate directory.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder ConfigureSslCertDir(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        // Set SSL_CERT_DIR to include both the system certs and the dev-certs trust path
        // Using $HOME instead of ~ for proper expansion in the shell
        return builder
            .Type("export SSL_CERT_DIR=\"/etc/ssl/certs:$HOME/.aspnet/dev-certs/trust\"")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Clears the terminal screen between test steps to avoid pattern interference.
    /// Requires TERM to be set (done in SetEnvironmentFromInstallerOutput).
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder ClearScreen(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        return builder
            .Type("clear")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Clears the first-time use sentinel file to simulate a fresh CLI installation.
    /// The sentinel is stored at ~/.aspire/cli/cli.firstUseSentinel and controls
    /// whether the welcome banner and telemetry notice are displayed.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder ClearFirstRunSentinel(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        // Remove the sentinel file to trigger first-time use behavior
        return builder
            .Type("rm -f ~/.aspire/cli/cli.firstUseSentinel")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Verifies that the first-time use sentinel file was successfully deleted.
    /// This is a debugging aid to help diagnose banner test failures.
    /// The command will fail if the sentinel file still exists after deletion.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder VerifySentinelDeleted(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        // Verify the sentinel file doesn't exist - this will return exit code 1 (ERR) if file exists
        // Using test -f which returns 0 if file exists, 1 if not. We negate with ! to fail if exists.
        return builder
            .Type("test ! -f ~/.aspire/cli/cli.firstUseSentinel")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Installs a specific GA version of the Aspire CLI using the install script.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="version">The version to install (e.g., "13.1.0").</param>
    /// <param name="counter">The sequence counter for prompt detection.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder InstallAspireCliVersion(
        this Hex1bTerminalInputSequenceBuilder builder,
        string version,
        SequenceCounter counter)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli.sh | bash -s -- --version \"{version}\"";

        return builder
            .Type(command)
            .Enter()
            .WaitForSuccessPromptFailFast(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Creates a deprecated MCP config file for testing migration detection.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="configPath">The path to create the config file.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder CreateDeprecatedMcpConfig(
        this Hex1bTerminalInputSequenceBuilder builder,
        string configPath)
    {
        var deprecatedConfig = """{"mcpServers":{"aspire":{"command":"aspire","args":["mcp","start"]}}}""";

        return builder.ExecuteCallback(() => File.WriteAllText(configPath, deprecatedConfig));
    }

    /// <summary>
    /// Creates a .vscode/mcp.json file with malformed content for testing error handling.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="configPath">The path to the mcp.json file.</param>
    /// <param name="content">The malformed content to write.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder CreateMalformedMcpConfig(
        this Hex1bTerminalInputSequenceBuilder builder,
        string configPath,
        string content = "{ invalid json content")
    {
        return builder.ExecuteCallback(() =>
        {
            var dir = Path.GetDirectoryName(configPath);
            if (dir is not null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(configPath, content);
        });
    }

    /// <summary>
    /// Creates a .vscode folder for testing VS Code agent detection.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="vscodePath">The path to the .vscode directory.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder CreateVsCodeFolder(
        this Hex1bTerminalInputSequenceBuilder builder,
        string vscodePath)
    {
        return builder.ExecuteCallback(() => Directory.CreateDirectory(vscodePath));
    }

    /// <summary>
    /// Verifies a file contains expected content.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="filePath">The path to the file to verify.</param>
    /// <param name="expectedContent">The content that should be present in the file.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder VerifyFileContains(
        this Hex1bTerminalInputSequenceBuilder builder,
        string filePath,
        string expectedContent)
    {
        return builder.ExecuteCallback(() =>
        {
            var content = File.ReadAllText(filePath);
            if (!content.Contains(expectedContent))
            {
                throw new InvalidOperationException(
                    $"File {filePath} does not contain expected content: {expectedContent}");
            }
        });
    }

    /// <summary>
    /// Verifies a file does NOT contain specified content.
    /// </summary>
    /// <param name="builder">The sequence builder.</param>
    /// <param name="filePath">The path to the file to verify.</param>
    /// <param name="unexpectedContent">The content that should NOT be present in the file.</param>
    /// <returns>The builder for chaining.</returns>
    internal static Hex1bTerminalInputSequenceBuilder VerifyFileDoesNotContain(
        this Hex1bTerminalInputSequenceBuilder builder,
        string filePath,
        string unexpectedContent)
    {
        return builder.ExecuteCallback(() =>
        {
            var content = File.ReadAllText(filePath);
            if (content.Contains(unexpectedContent))
            {
                throw new InvalidOperationException(
                    $"File {filePath} unexpectedly contains: {unexpectedContent}");
            }
        });
    }

    /// <summary>
    /// Specifies how the Aspire CLI should be installed inside a Docker container.
    /// </summary>
    internal enum DockerInstallMode
    {
        /// <summary>
        /// The CLI was built from source by the Dockerfile and is already on PATH.
        /// </summary>
        SourceBuild,

        /// <summary>
        /// Install the latest GA release from aspire.dev.
        /// </summary>
        GaRelease,

        /// <summary>
        /// Install from PR artifacts using the get-aspire-cli-pr.sh script.
        /// </summary>
        PullRequest,
    }

    /// <summary>
    /// Detects the install mode for Docker-based tests based on the current environment.
    /// </summary>
    /// <param name="repoRoot">The repo root directory on the host.</param>
    /// <returns>The detected <see cref="DockerInstallMode"/>.</returns>
    internal static DockerInstallMode DetectDockerInstallMode(string repoRoot)
    {
        if (IsRunningInCI)
        {
            return DockerInstallMode.PullRequest;
        }

        // Check if local build artifacts exist (developer has built the CLI locally).
        var cliPublishDir = Path.Combine(repoRoot, "artifacts", "bin", "Aspire.Cli");
        if (Directory.Exists(cliPublishDir))
        {
            return DockerInstallMode.SourceBuild;
        }

        return DockerInstallMode.GaRelease;
    }

    /// <summary>
    /// Creates a Hex1b terminal that runs inside a Docker container built from the E2E Dockerfile.
    /// The Dockerfile builds the CLI from source (local dev) or accepts pre-built artifacts (CI).
    /// </summary>
    /// <param name="repoRoot">The repo root directory, used as the Docker build context.</param>
    /// <param name="installMode">The detected install mode, controlling Docker build args and volumes.</param>
    /// <param name="width">Terminal width in columns.</param>
    /// <param name="height">Terminal height in rows.</param>
    /// <param name="testName">The test name for the recording file path.</param>
    /// <returns>A configured <see cref="Hex1bTerminal"/>. Caller is responsible for disposal.</returns>
    internal static Hex1bTerminal CreateDockerTestTerminal(
        string repoRoot,
        DockerInstallMode installMode,
        int width = 160,
        int height = 48,
        [CallerMemberName] string testName = "")
    {
        var recordingPath = GetTestResultsRecordingPath(testName);
        var dockerfilePath = Path.Combine(repoRoot, "tests", "Aspire.Cli.EndToEnd.Tests", "docker", "Dockerfile.e2e");

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithDimensions(width, height)
            .WithAsciinemaRecording(recordingPath)
            .WithDockerContainer(c =>
            {
                c.DockerfilePath = dockerfilePath;
                c.BuildContext = repoRoot;

                if (installMode != DockerInstallMode.SourceBuild)
                {
                    // Skip the source build stage when we'll install via script instead.
                    c.BuildArgs["SKIP_SOURCE_BUILD"] = "true";
                }
            });

        return builder.Build();
    }

    /// <summary>
    /// Sets up the bash prompt tracking inside a Docker container.
    /// Docker containers run as root, so the default prompt uses '#' instead of '$'.
    /// This method waits for the container prompt and configures the sequence counter.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder PrepareDockerEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        // Docker containers run as root, so bash shows '# ' (not '$ ').
        // With --norc, the prompt may be 'bash-X.Y# ' or 'root@host:/# '.
        var waitingForContainerReady = new CellPatternSearcher()
            .Find("# ");

        builder
            .WaitUntil(s => waitingForContainerReady.Search(s).Count > 0, TimeSpan.FromSeconds(60))
            .Wait(500);

        // Set up the same prompt counting mechanism used by all E2E tests.
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";

        return builder
            .Type(promptSetup)
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Installs the Aspire CLI inside a Docker container based on the detected install mode.
    /// For <see cref="DockerInstallMode.SourceBuild"/>, the CLI is already installed by the Dockerfile.
    /// For <see cref="DockerInstallMode.GaRelease"/>, downloads and installs from aspire.dev.
    /// For <see cref="DockerInstallMode.PullRequest"/>, uses the PR install script.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder InstallAspireCliInDocker(
        this Hex1bTerminalInputSequenceBuilder builder,
        DockerInstallMode installMode,
        SequenceCounter counter)
    {
        switch (installMode)
        {
            case DockerInstallMode.SourceBuild:
                // CLI was built from source by the Dockerfile and is already on PATH.
                // Just verify it's accessible.
                return builder
                    .Type("which aspire")
                    .Enter()
                    .WaitForSuccessPrompt(counter);

            case DockerInstallMode.GaRelease:
                // Install the latest GA release from aspire.dev.
                return builder
                    .Type("curl -sSL https://aspire.dev/install.sh | bash")
                    .Enter()
                    .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(120))
                    .Type("export PATH=~/.aspire/bin:$PATH")
                    .Enter()
                    .WaitForSuccessPrompt(counter);

            case DockerInstallMode.PullRequest:
                var prNumber = GetRequiredPrNumber();
                builder.InstallAspireCliFromPullRequest(prNumber, counter);
                builder.SourceAspireCliEnvironment(counter);
                return builder;

            default:
                throw new ArgumentOutOfRangeException(nameof(installMode));
        }
    }
}
