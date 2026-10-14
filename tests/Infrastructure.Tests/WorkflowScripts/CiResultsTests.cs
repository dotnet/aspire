// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests.WorkflowScripts;

public sealed class CiResultsTests(ITestOutputHelper output)
{
    private const string ScriptPath = ".github/scripts/check-ci-results.js";

    public static TheoryData<string, string, string, string, string, string, string, int> Scenarios => new()
    {
        { "infrastructure-only pull request skips stabilization", "pull_request", "success", "false", "false", "success", "skipped", 0 },
        { "source pull request requires stabilization", "pull_request", "success", "false", "true", "success", "success", 0 },
        { "push requires stabilization", "push", "success", "", "", "success", "success", 0 },
        { "intentionally skipped pull request passes", "pull_request", "success", "true", "false", "skipped", "skipped", 0 },
        { "test failure fails", "pull_request", "success", "false", "false", "failure", "skipped", 1 },
        { "test cancellation fails", "pull_request", "success", "false", "false", "cancelled", "skipped", 1 },
        { "unexpectedly skipped tests fail", "pull_request", "success", "false", "false", "skipped", "skipped", 1 },
        { "required stabilization failure fails", "pull_request", "success", "false", "true", "success", "failure", 1 },
        { "required stabilization cancellation fails", "pull_request", "success", "false", "true", "success", "cancelled", 1 },
        { "required stabilization skip fails", "pull_request", "success", "false", "true", "success", "skipped", 1 },
        { "prepare failure fails", "pull_request", "failure", "true", "false", "skipped", "skipped", 1 },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    [RequiresTools(["node"])]
    public async Task ResultGateRequiresEveryApplicableCiJobToSucceed(
        string _,
        string eventName,
        string prepareResult,
        string skipWorkflow,
        string stabilizationRequired,
        string testsResult,
        string stabilizationResult,
        int expectedExitCode)
    {
        using var node = new NodeCommand(output, nameof(CiResultsTests))
            .WithWorkingDirectory(RepoRoot.Path)
            .WithEnvironmentVariable("CI_EVENT_NAME", eventName)
            .WithEnvironmentVariable("PREPARE_RESULT", prepareResult)
            .WithEnvironmentVariable("SKIP_WORKFLOW", skipWorkflow)
            .WithEnvironmentVariable("STABILIZATION_REQUIRED", stabilizationRequired)
            .WithEnvironmentVariable("TESTS_RESULT", testsResult)
            .WithEnvironmentVariable("STABILIZATION_RESULT", stabilizationResult);

        var result = await node.ExecuteScriptAsync(Path.Combine(RepoRoot.Path, ScriptPath));

        Assert.Equal(expectedExitCode, result.ExitCode);
    }
}
