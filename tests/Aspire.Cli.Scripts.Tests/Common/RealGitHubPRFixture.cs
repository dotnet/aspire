// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Test fixture that finds a real merged PR with required artifacts for integration testing.
/// Throws InvalidOperationException if no suitable PR is found.
/// </summary>
public sealed class RealGitHubPRFixture : IDisposable
{
    private const string RequiredArtifactPrefix = "cli-native-";
    private const string RequiredBuiltNugets = "built-nugets";
    private const string Repository = "dotnet/aspire";

    public int PRNumber { get; private set; }
    public long WorkflowRunId { get; private set; }

    public RealGitHubPRFixture()
    {
        FindSuitablePR();
    }

    private void FindSuitablePR()
    {
        // Use gh CLI to find a recent merged PR with the required artifacts
        // We search for merged PRs and check their artifacts
        // To avoid complex jq filters, we get JSON output and parse in C#

        var psi = new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = $"pr list --repo {Repository} --state merged --limit 50 --json number,mergedAt",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start gh process");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"gh CLI failed: {error}");
        }

        var prs = JsonSerializer.Deserialize<List<PRInfo>>(output)
            ?? throw new InvalidOperationException("Failed to parse PR list from gh CLI");

        // Try each PR until we find one with the required artifacts
        foreach (var pr in prs.OrderByDescending(p => p.MergedAt))
        {
            if (TryGetWorkflowRunWithArtifacts(pr.Number, out var workflowRunId))
            {
                PRNumber = pr.Number;
                WorkflowRunId = workflowRunId;
                return;
            }
        }

        throw new InvalidOperationException(
            $"No suitable PR found in the last 50 merged PRs. " +
            $"Required artifacts: {RequiredArtifactPrefix}*, {RequiredBuiltNugets}");
    }

    private bool TryGetWorkflowRunWithArtifacts(int prNumber, out long workflowRunId)
    {
        workflowRunId = 0;

        try
        {
            // Get workflow runs for this PR
            var psi = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = $"run list --repo {Repository} --workflow=ci.yml --json databaseId,conclusion,displayTitle --limit 10",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return false;
            }

            var runs = JsonSerializer.Deserialize<List<WorkflowRunInfo>>(output);
            if (runs == null)
            {
                return false;
            }

            // Find a successful run for this PR
            foreach (var run in runs.Where(r => r.Conclusion == "success" && r.DisplayTitle.Contains($"#{prNumber}")))
            {
                if (CheckArtifacts(run.DatabaseId))
                {
                    workflowRunId = run.DatabaseId;
                    return true;
                }
            }
        }
        catch
        {
            // If we can't check this PR, try the next one
        }

        return false;
    }

    private static bool CheckArtifacts(long runId)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = $"run view {runId} --repo {Repository} --json artifacts",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return false;
            }

            var runInfo = JsonSerializer.Deserialize<WorkflowRunArtifacts>(output);
            if (runInfo?.Artifacts == null)
            {
                return false;
            }

            var artifactNames = runInfo.Artifacts.Select(a => a.Name).ToList();

            // Check for required artifacts
            var hasCliArchive = artifactNames.Any(n => n.StartsWith(RequiredArtifactPrefix, StringComparison.OrdinalIgnoreCase));
            var hasBuiltNugets = artifactNames.Any(n => n.Equals(RequiredBuiltNugets, StringComparison.OrdinalIgnoreCase));

            return hasCliArchive && hasBuiltNugets;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private sealed class PRInfo
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("mergedAt")]
        public DateTime MergedAt { get; set; }
    }

    private sealed class WorkflowRunInfo
    {
        [JsonPropertyName("databaseId")]
        public long DatabaseId { get; set; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; set; } = "";

        [JsonPropertyName("displayTitle")]
        public string DisplayTitle { get; set; } = "";
    }

    private sealed class WorkflowRunArtifacts
    {
        [JsonPropertyName("artifacts")]
        public List<ArtifactInfo> Artifacts { get; set; } = [];
    }

    private sealed class ArtifactInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
