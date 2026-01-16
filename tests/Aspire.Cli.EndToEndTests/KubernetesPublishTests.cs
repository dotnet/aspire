// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI publishing to Kubernetes/Helm.
/// Tests the complete workflow: create project, add Kubernetes integration, publish, and verify Helm chart generation.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class KubernetesPublishTests(ITestOutputHelper output)
{
    private const string ProjectName = "AspireKubernetesPublishTest";

    [Fact]
    public async Task CreateAndPublishToKubernetes()
    {
        using var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateAndPublishToKubernetes));

        var builder = Hex1bTerminal.CreateBuilder()
            .WithHeadless()
            .WithAsciinemaRecording(recordingPath)
            .WithPtyProcess("/bin/bash", ["--norc"]);

        using var terminal = builder.Build();

        var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

        // Pattern searchers for template selection
        var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
            .FindPattern("> Starter App");

        // In CI, aspire add shows a version selection prompt (but aspire new does not when channel is set)
        var waitingForAddVersionSelectionPrompt = new CellPatternSearcher()
            .Find("(based on NuGet.config)");

        var waitingForProjectNamePrompt = new CellPatternSearcher()
            .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

        var waitingForOutputPathPrompt = new CellPatternSearcher()
            .Find($"Enter the output path: (./{ProjectName}): ");

        var waitingForUrlsPrompt = new CellPatternSearcher()
            .Find($"Use *.dev.localhost URLs");

        var waitingForRedisPrompt = new CellPatternSearcher()
            .Find($"Use Redis Cache");

        var waitingForTestPrompt = new CellPatternSearcher()
            .Find($"Do you want to create a test project?");

        var counter = new SequenceCounter();
        var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

        sequenceBuilder.PrepareEnvironment(workspace, counter);

        if (isCI)
        {
            sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
            sequenceBuilder.SourceAspireCliEnvironment(counter);
            sequenceBuilder.VerifyAspireCliVersion(commitSha, counter);
        }

        // Step 1: Create a new Aspire Starter App
        // Note: When channel is set (CI), aspire new auto-selects version - no version prompt appears
        sequenceBuilder.Type("aspire new")
            .Enter()
            .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
            .Enter() // select first template (Starter App)
            .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Type(ProjectName)
            .Enter()
            .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // accept default output path
            .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            .Enter() // select "No" for localhost URLs (default)
            .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            // For Redis prompt, default is "Yes" so we need to select "No" by pressing Down
            .Key(Hex1b.Input.Hex1bKey.DownArrow)
            .Enter() // select "No" for Redis Cache
            .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
            // For test project prompt, default is "No" so just press Enter to accept it
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 2: Navigate into the project directory
        sequenceBuilder.Type($"cd {ProjectName}")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 3: Add Aspire.Hosting.Kubernetes package using aspire add
        // Pass the package name directly as an argument to avoid interactive selection
        sequenceBuilder.Type("aspire add Aspire.Hosting.Kubernetes")
            .Enter();

        // In CI, aspire add shows a version selection prompt (unlike aspire new which auto-selects when channel is set)
        if (isCI)
        {
            sequenceBuilder
                .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter(); // select first version (PR build)
        }

        sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

        // Step 4: Modify AppHost's main file to add Kubernetes environment
        // We'll use a callback to modify the file during sequence execution
        // Note: Aspire templates use AppHost.cs as the main entry point, not Program.cs
        sequenceBuilder.ExecuteCallback(() =>
        {
            var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, ProjectName);
            var appHostDir = Path.Combine(projectDir, $"{ProjectName}.AppHost");
            var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

            output.WriteLine($"Looking for AppHost.cs at: {appHostFilePath}");

            var content = File.ReadAllText(appHostFilePath);

            // Insert the Kubernetes environment before builder.Build().Run();
            var buildRunPattern = "builder.Build().Run();";
            var replacement = """
// Add Kubernetes environment for Helm chart generation
builder.AddKubernetesEnvironment("env");

builder.Build().Run();
""";

            content = content.Replace(buildRunPattern, replacement);
            File.WriteAllText(appHostFilePath, content);

            output.WriteLine($"Modified AppHost.cs at: {appHostFilePath}");
        });

        // Step 5: Create output directory for Helm chart artifacts
        sequenceBuilder.Type("mkdir -p helm-output")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 6: Unset ASPIRE_PLAYGROUND before publish
        // ASPIRE_PLAYGROUND=true takes precedence over --non-interactive in CliHostEnvironment,
        // which causes Spectre.Console to try to show interactive spinners and prompts concurrently,
        // resulting in "Operations with dynamic displays cannot run at the same time" errors.
        sequenceBuilder.Type("unset ASPIRE_PLAYGROUND")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 7: Run aspire publish to generate Helm charts
        // This will build the project and generate Kubernetes manifests as Helm charts
        // Use --non-interactive to avoid any prompts during publishing
        sequenceBuilder.Type("aspire publish -o helm-output --non-interactive")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

        // Step 8: Verify the Helm chart files were generated
        // Check for Chart.yaml (required Helm chart metadata)
        sequenceBuilder.Type("cat helm-output/Chart.yaml")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 9: Verify values.yaml exists (Helm values file)
        sequenceBuilder.Type("cat helm-output/values.yaml")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 10: Verify templates directory exists with Kubernetes manifests
        sequenceBuilder.Type("ls -la helm-output/templates/")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 11: Display the directory structure for debugging
        sequenceBuilder.Type("find helm-output -type f | head -20")
            .Enter()
            .WaitForSuccessPrompt(counter);

        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
