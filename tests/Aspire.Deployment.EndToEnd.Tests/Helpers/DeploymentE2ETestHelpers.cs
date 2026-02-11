// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;

namespace Aspire.Deployment.EndToEnd.Tests.Helpers;

/// <summary>
/// Helper methods for creating and managing Hex1b terminal sessions for deployment testing.
/// Extends the patterns from CLI E2E tests with deployment-specific functionality.
/// </summary>
internal static class DeploymentE2ETestHelpers
{
    /// <summary>
    /// Gets whether the tests are running in CI (GitHub Actions) vs locally.
    /// </summary>
    internal static bool IsRunningInCI =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

    /// <summary>
    /// Gets the PR number from the GITHUB_PR_NUMBER environment variable.
    /// When running locally (not in CI), returns 0.
    /// </summary>
    internal static int GetPrNumber()
    {
        var prNumberStr = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER");
        if (string.IsNullOrEmpty(prNumberStr) || !int.TryParse(prNumberStr, out var prNumber))
        {
            return 0;
        }
        return prNumber;
    }

    /// <summary>
    /// Gets the commit SHA from the GITHUB_PR_HEAD_SHA environment variable.
    /// When running locally (not in CI), returns "local0000".
    /// </summary>
    internal static string GetCommitSha()
    {
        var commitSha = Environment.GetEnvironmentVariable("GITHUB_PR_HEAD_SHA");
        return string.IsNullOrEmpty(commitSha) ? "local0000" : commitSha;
    }

    /// <summary>
    /// Gets the GitHub Actions run ID from the GITHUB_RUN_ID environment variable.
    /// When running locally (not in CI), returns a timestamp-based ID.
    /// </summary>
    internal static string GetRunId()
    {
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        return string.IsNullOrEmpty(runId) ? DateTime.UtcNow.ToString("yyyyMMddHHmmss") : runId;
    }

    /// <summary>
    /// Gets the GitHub Actions run attempt from the GITHUB_RUN_ATTEMPT environment variable.
    /// When running locally (not in CI), returns "1".
    /// </summary>
    internal static string GetRunAttempt()
    {
        var runAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT");
        return string.IsNullOrEmpty(runAttempt) ? "1" : runAttempt;
    }

    /// <summary>
    /// Generates a unique resource group name for deployment tests.
    /// Format: e2e-[testcasename]-[runid]-[attempt]
    /// </summary>
    /// <param name="testCaseName">Short name for the test case (e.g., "starter", "python").</param>
    /// <returns>A unique resource group name.</returns>
    internal static string GenerateResourceGroupName(string testCaseName)
    {
        var runId = GetRunId();
        var attempt = GetRunAttempt();
        return $"e2e-{testCaseName}-{runId}-{attempt}";
    }

    /// <summary>
    /// Gets the path for storing asciinema recordings that will be uploaded as CI artifacts.
    /// </summary>
    internal static string GetTestResultsRecordingPath(string testName)
    {
        var githubWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        string recordingsDir;

        if (!string.IsNullOrEmpty(githubWorkspace))
        {
            recordingsDir = Path.Combine(githubWorkspace, "testresults", "recordings");
        }
        else
        {
            recordingsDir = Path.Combine(Path.GetTempPath(), "aspire-deployment-e2e", "recordings");
        }

        Directory.CreateDirectory(recordingsDir);
        return Path.Combine(recordingsDir, $"{testName}.cast");
    }

    /// <summary>
    /// Gets the path for the GitHub step summary file.
    /// Returns null when not running in CI.
    /// </summary>
    internal static string? GetGitHubStepSummaryPath()
    {
        return Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    }

    /// <summary>
    /// Prepares the terminal environment with a custom prompt for command tracking.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder PrepareEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder,
        TemporaryWorkspace workspace,
        SequenceCounter counter)
    {
        var waitingForInputPattern = new CellPatternSearcher()
            .Find("b").RightUntil("$").Right(' ').Right(' ');

        builder.WaitUntil(s => waitingForInputPattern.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Wait(500);

        // Bash prompt setup with command tracking
        const string promptSetup = "CMDCOUNT=0; PROMPT_COMMAND='s=$?;((CMDCOUNT++));PS1=\"[$CMDCOUNT $([ $s -eq 0 ] && echo OK || echo ERR:$s)] \\$ \"'";
        builder.Type(promptSetup).Enter();

        return builder.WaitForSuccessPrompt(counter)
            .Type($"cd {workspace.WorkspaceRoot.FullName}").Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Installs the Aspire CLI from PR build artifacts.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder InstallAspireCliFromPullRequest(
        this Hex1bTerminalInputSequenceBuilder builder,
        int prNumber,
        SequenceCounter counter)
    {
        var command = $"curl -fsSL https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-pr.sh | bash -s -- {prNumber}";

        return builder
            .Type(command)
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Installs the latest GA (release quality) Aspire CLI.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder InstallAspireCliRelease(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        var command = "curl -fsSL https://aka.ms/aspire/get/install.sh | bash -s -- --quality release";

        return builder
            .Type(command)
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Configures the PATH and environment variables for the Aspire CLI.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder SourceAspireCliEnvironment(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        return builder
            .Type("export PATH=~/.aspire/bin:$PATH ASPIRE_PLAYGROUND=true DOTNET_CLI_TELEMETRY_OPTOUT=true DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_GENERATE_ASPNET_CERTIFICATE=false")
            .Enter()
            .WaitForSuccessPrompt(counter);
    }

    /// <summary>
    /// Waits for a successful command prompt with the expected sequence number.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder WaitForSuccessPrompt(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(500);

        return builder.WaitUntil(snapshot =>
            {
                var successPromptSearcher = new CellPatternSearcher()
                    .FindPattern(counter.Value.ToString())
                    .RightText(" OK] $ ");

                var result = successPromptSearcher.Search(snapshot);
                return result.Count > 0;
            }, effectiveTimeout)
            .IncrementSequence(counter);
    }

    /// <summary>
    /// Increments the sequence counter.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder IncrementSequence(
        this Hex1bTerminalInputSequenceBuilder builder,
        SequenceCounter counter)
    {
        return builder.WaitUntil(s =>
        {
            counter.Increment();
            return true;
        }, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Executes an arbitrary callback action during the sequence execution.
    /// </summary>
    internal static Hex1bTerminalInputSequenceBuilder ExecuteCallback(
        this Hex1bTerminalInputSequenceBuilder builder,
        Action callback)
    {
        return builder.WaitUntil(s =>
        {
            callback();
            return true;
        }, TimeSpan.FromSeconds(1));
    }
}
