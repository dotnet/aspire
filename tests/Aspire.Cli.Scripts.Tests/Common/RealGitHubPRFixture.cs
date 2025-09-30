// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Templates.Tests;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Fixture that finds a real PR from dotnet/aspire with completed builds and required artifacts.
/// This is used for integration tests that actually download and install from a PR.
/// </summary>
public class RealGitHubPRFixture : IAsyncLifetime
{
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TestOutputWrapper _testOutput;

    public int? PRNumber { get; private set; }
    public string Repository { get; } = "dotnet/aspire";

    public RealGitHubPRFixture(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink, forceShowBuildOutput: true);
    }

    public async ValueTask InitializeAsync()
    {
        // Use gh cli to find a suitable PR
        PRNumber = await FindSuitablePRAsync();
        
        if (PRNumber == null)
        {
            throw new InvalidOperationException(
                "Could not find a suitable PR for testing. " +
                "Ensure gh cli is authenticated and there are recent merged PRs with required artifacts " +
                "(cli-native-*, built-nugets, built-nugets-for*).");
        }
        
        _testOutput.WriteLine($"Found suitable PR: {PRNumber}");
    }

    private async Task<int?> FindSuitablePRAsync()
    {
        try
        {
            // Query for recent merged PRs using gh cli
            var command = new ToolCommand("gh", _testOutput)
                .WithTimeout(TimeSpan.FromSeconds(30));

            var result = await command.ExecuteAsync(
                "pr", "list",
                "--repo", Repository,
                "--state", "all",
                "--limit", "50",
                "--json", "number,state,mergedAt,headRefOid",
                "--jq", "[.[] | select(.mergedAt != null)] | .[0:10] | .[] | .number"
            );

            if (result.ExitCode != 0)
            {
                _testOutput.WriteLine($"gh cli failed: {result.Output}");
                return null;
            }

            // Parse the PR numbers from output
            var prNumbers = result.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => int.TryParse(line.Trim(), out var num) ? (int?)num : null)
                .Where(n => n.HasValue)
                .Select(n => n!.Value)
                .ToList();

            // Check each PR for required artifacts
            foreach (var prNumber in prNumbers)
            {
                if (await HasRequiredArtifactsAsync(prNumber))
                {
                    return prNumber;
                }
            }

            _testOutput.WriteLine("No PR found with required artifacts");
            return null;
        }
        catch (Exception ex)
        {
            _testOutput.WriteLine($"Error finding suitable PR: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> HasRequiredArtifactsAsync(int prNumber)
    {
        try
        {
            var command = new ToolCommand("gh", _testOutput)
                .WithTimeout(TimeSpan.FromSeconds(30));

            // Get workflow runs for this PR
            var result = await command.ExecuteAsync(
                "run", "list",
                "--repo", Repository,
                "--json", "databaseId,conclusion,displayTitle",
                "--limit", "20",
                "--jq", $"[.[] | select(.displayTitle | contains(\"#{prNumber}\")) | select(.conclusion == \"success\")] | .[0].databaseId"
            );

            if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.Output))
            {
                return false;
            }

            var runId = result.Output.Trim();
            if (string.IsNullOrEmpty(runId) || runId == "null")
            {
                return false;
            }

            // Check for required artifacts
            var artifactsResult = await command.ExecuteAsync(
                "run", "view", runId,
                "--repo", Repository,
                "--json", "artifacts",
                "--jq", ".artifacts[].name"
            );

            if (artifactsResult.ExitCode != 0)
            {
                return false;
            }

            var artifacts = artifactsResult.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var hasCliNative = artifacts.Any(a => a.StartsWith("cli-native-", StringComparison.OrdinalIgnoreCase));
            var hasBuiltNugets = artifacts.Any(a => a.Equals("built-nugets", StringComparison.OrdinalIgnoreCase));
            var hasBuiltNugetsFor = artifacts.Any(a => a.StartsWith("built-nugets-for", StringComparison.OrdinalIgnoreCase));

            return hasCliNative && hasBuiltNugets && hasBuiltNugetsFor;
        }
        catch (Exception ex)
        {
            _testOutput.WriteLine($"Error checking artifacts for PR {prNumber}: {ex.Message}");
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
