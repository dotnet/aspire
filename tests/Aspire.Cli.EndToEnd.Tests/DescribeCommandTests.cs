// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for the aspire describe command.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class DescribeCommandTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DescribeCommandShowsRunningResources()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .Find("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./AspireResourcesTestApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        // Pattern searchers for start/stop/resources commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        // Pattern for aspire resources output - table header
        var waitForResourcesTableHeader = new CellPatternSearcher()
            .Find("Name");

        // Pattern for resources - should show the webfrontend and apiservice
        var waitForWebfrontendResource = new CellPatternSearcher()
            .Find("webfrontend");

        var waitForApiserviceResource = new CellPatternSearcher()
            .Find("apiservice");

        // Pattern for verifying JSON output was written to file
        var waitForJsonFileWritten = new CellPatternSearcher()
            .Find("webfrontend");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create a new project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("AspireResourcesTestApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspireResourcesTestApp/AspireResourcesTestApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Wait a bit for resources to stabilize
        sequenceBuilder.Type("sleep 5")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Now verify aspire describe shows the running resources (human-readable table)
        sequenceBuilder.Type("aspire describe")
            .Enter()
            .WaitUntil(s => waitForResourcesTableHeader.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .WaitUntil(s => waitForWebfrontendResource.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .WaitUntil(s => waitForApiserviceResource.Search(s).Count > 0, TimeSpan.FromSeconds(5))
            .WaitForSuccessPrompt(counter);

        // Test aspire describe --format json output - pipe to file to avoid terminal buffer issues
        sequenceBuilder.Type("aspire describe --format json > resources.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the JSON file contains expected resources
        sequenceBuilder.Type("cat resources.json | grep webfrontend")
            .Enter()
            .WaitUntil(s => waitForJsonFileWritten.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Stop the AppHost using aspire stop
        sequenceBuilder.Type("aspire stop")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }

    [Fact]
    public async Task DescribeCommandResolvesReplicaNames()
    {
        var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        using var terminal = CliE2ETestHelpers.CreateTestTerminal();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for aspire new prompts
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .Find("> Starter App");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./AspireReplicaTestApp): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        // Pattern searchers for start/stop commands
        var waitForAppHostStartedSuccessfully = new CellPatternSearcher()
            .Find("AppHost started successfully.");

        var waitForAppHostStoppedSuccessfully = new CellPatternSearcher()
            .Find("AppHost stopped successfully.");

        // Pattern for describe output with friendly name (non-replicated resource)
        var waitForCacheResource = new CellPatternSearcher()
            .Find("cache");

        // Pattern for describe output showing a specific replica
        var waitForApiserviceReplicaName = new CellPatternSearcher()
            .FindPattern("apiservice-[a-z0-9]+");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Create a new project using aspire new
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type("AspireReplicaTestApp")
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Navigate to the AppHost directory
        sequenceBuilder.Type("cd AspireReplicaTestApp/AspireReplicaTestApp.AppHost")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Add .WithReplicas(2) to the apiservice resource in the AppHost
        sequenceBuilder.ExecuteCallback(() =>
        {
            var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, "AspireReplicaTestApp");
            var appHostDir = Path.Combine(projectDir, "AspireReplicaTestApp.AppHost");
            var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

            output.WriteLine($"Looking for AppHost.cs at: {appHostFilePath}");

            var content = File.ReadAllText(appHostFilePath);

            // Add .WithReplicas(2) to the first .WithHttpHealthCheck("/health"); occurrence (apiservice)
            var originalPattern = ".WithHttpHealthCheck(\"/health\");";
            var replacement = ".WithHttpHealthCheck(\"/health\").WithReplicas(2);";

            // Only replace the first occurrence (apiservice), not the second (webfrontend)
            var index = content.IndexOf(originalPattern);
            if (index >= 0)
            {
                content = content[..index] + replacement + content[(index + originalPattern.Length)..];
            }

            File.WriteAllText(appHostFilePath, content);

            output.WriteLine($"Modified AppHost.cs to add .WithReplicas(2) to apiservice");
        });

        // Start the AppHost in the background using aspire run --detach
        sequenceBuilder.Type("aspire run --detach")
            .Enter()
            .WaitUntil(s => waitForAppHostStartedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(3))
            .WaitForSuccessPrompt(counter);

        // Wait for resources to stabilize
        sequenceBuilder.Type("sleep 10")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Test 1: aspire describe with friendly name for a non-replicated resource (cache)
        // This should resolve via DisplayName since cache has only one instance
        sequenceBuilder.Type("aspire describe cache --format json > cache-describe.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify cache resource was found in the output
        sequenceBuilder.Type("cat cache-describe.json | grep cache")
            .Enter()
            .WaitUntil(s => waitForCacheResource.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Test 2: Get all resources to find an apiservice replica name
        sequenceBuilder.Type("aspire describe --format json > all-resources.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Extract a replica name from the JSON - apiservice replicas have names like apiservice-<suffix>
        sequenceBuilder.Type("REPLICA_NAME=$(cat all-resources.json | grep -o '\"name\": *\"apiservice-[a-z0-9]*\"' | head -1 | sed 's/.*\"\\(apiservice-[a-z0-9]*\\)\"/\\1/')")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify we captured a replica name
        sequenceBuilder.Type("echo \"Found replica: $REPLICA_NAME\"")
            .Enter()
            .WaitUntil(s => waitForApiserviceReplicaName.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Test 3: aspire describe with the replica name
        // This should resolve via exact Name match
        sequenceBuilder.Type("aspire describe $REPLICA_NAME --format json > replica-describe.json")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Verify the replica was found and output contains the replica name
        sequenceBuilder.Type("cat replica-describe.json | grep apiservice")
            .Enter()
            .WaitUntil(s => waitForApiserviceReplicaName.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .WaitForSuccessPrompt(counter);

        // Stop the AppHost using aspire stop
        sequenceBuilder.Type("aspire stop")
            .Enter()
            .WaitUntil(s => waitForAppHostStoppedSuccessfully.Search(s).Count > 0, TimeSpan.FromMinutes(1))
            .WaitForSuccessPrompt(counter);

        // Exit the shell
        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
