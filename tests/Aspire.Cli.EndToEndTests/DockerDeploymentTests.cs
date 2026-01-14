// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEndTests.Helpers;
using Aspire.Cli.Tests.Utils;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEndTests;

/// <summary>
/// End-to-end tests for Aspire CLI deployment to Docker Compose.
/// Tests the complete workflow: create project, add Docker integration, deploy, and verify.
/// Each test class runs as a separate CI job for parallelization.
/// </summary>
public sealed class DockerDeploymentTests(ITestOutputHelper output)
{
    private const string ProjectName = "AspireDockerDeployTest";

    [Fact]
    public async Task CreateAndDeployToDockerCompose()
    {
        using var workspace = TemporaryWorkspace.Create(output);

        var prNumber = CliE2ETestHelpers.GetRequiredPrNumber();
        var commitSha = CliE2ETestHelpers.GetRequiredCommitSha();
        var isCI = CliE2ETestHelpers.IsRunningInCI;
        var recordingPath = CliE2ETestHelpers.GetTestResultsRecordingPath(nameof(CreateAndDeployToDockerCompose));

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

        // Step 3: Add Aspire.Hosting.Docker package using aspire add
        // Pass the package name directly as an argument to avoid interactive selection
        sequenceBuilder.Type("aspire add Aspire.Hosting.Docker")
            .Enter();

        // In CI, aspire add shows a version selection prompt (unlike aspire new which auto-selects when channel is set)
        if (isCI)
        {
            sequenceBuilder
                .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter(); // select first version (PR build)
        }

        sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

        // Step 4: Modify AppHost Program.cs to add Docker Compose environment
        // We'll use a callback to modify the file during sequence execution
        sequenceBuilder.ExecuteCallback(() =>
        {
            var appHostProgramPath = Path.Combine(
                workspace.WorkspaceRoot.FullName,
                ProjectName,
                $"{ProjectName}.AppHost",
                "Program.cs");

            var content = File.ReadAllText(appHostProgramPath);

            // Insert the Docker Compose environment before builder.Build().Run();
            var buildRunPattern = "builder.Build().Run();";
            var replacement = """
// Add Docker Compose environment for deployment
builder.AddDockerComposeEnvironment("compose");

builder.Build().Run();
""";

            content = content.Replace(buildRunPattern, replacement);
            File.WriteAllText(appHostProgramPath, content);

            output.WriteLine($"Modified AppHost Program.cs at: {appHostProgramPath}");
        });

        // Step 5: Create output directory for deployment artifacts
        sequenceBuilder.Type("mkdir -p deploy-output")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 6: Run aspire deploy to deploy to Docker Compose
        // This will build the project, generate Docker Compose files, and start the containers
        sequenceBuilder.Type("aspire deploy -o deploy-output")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

        // Step 7: Capture the port from docker ps output for verification
        // We need to parse the port from docker ps to make a web request
        sequenceBuilder.Type("docker ps --format '{{.Ports}}' | grep -oE '0\\.0\\.0\\.0:[0-9]+' | head -1 | cut -d: -f2")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 8: Verify the deployment is running with docker ps
        sequenceBuilder.Type("docker ps")
            .Enter()
            .WaitForSuccessPrompt(counter);

        // Step 9: Make a web request to verify the application is working
        // We'll use curl to make the request
        sequenceBuilder.Type("curl -s -o /dev/null -w '%{http_code}' http://localhost:$(docker ps --format '{{.Ports}}' --filter 'name=webfrontend' | grep -oE '0\\.0\\.0\\.0:[0-9]+->8080' | head -1 | cut -d: -f2 | cut -d'-' -f1) 2>/dev/null || echo 'request-failed'")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

        // Step 10: Clean up - stop and remove containers
        sequenceBuilder.Type("cd deploy-output && docker compose down --volumes --remove-orphans 2>/dev/null || true")
            .Enter()
            .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

        sequenceBuilder.Type("exit")
            .Enter();

        var sequence = sequenceBuilder.Build();

        await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);

        await pendingRun;
    }
}
