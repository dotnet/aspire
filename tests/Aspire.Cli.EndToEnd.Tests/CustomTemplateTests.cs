// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

public sealed class CustomTemplateTests(ITestOutputHelper output)
{
    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task DirectoryTemplatePromptsWithoutDotNetSdk() => CreateFromTemplateAsync(repository: false);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task RepositoryTemplatePromptsWithoutDotNetSdk() => CreateFromTemplateAsync(repository: true);

    private async Task CreateFromTemplateAsync(bool repository)
    {
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var testDirectory = Path.Combine(repoRoot, "tests", "Aspire.Cli.EndToEnd.Tests");
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        var workspace = TemporaryWorkspace.Create(output);
        TestDirectoryHelpers.CopyDirectory(
            Path.Combine(testDirectory, "Fixtures", "CustomTemplate"),
            Path.Combine(workspace.WorkspaceRoot.FullName, "template"));

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(
            repoRoot, strategy, output, variant: CliE2ETestHelpers.DockerfileVariant.Polyglot, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(
            terminal, workspace, auto, counter, output, TestContext.Current.CancellationToken);

        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.RunCommandAsync("! command -v dotnet", counter);

        if (repository)
        {
            await auto.RunCommandAsync(
                "git -C template init && git -C template add . && " +
                "git -C template -c user.name='Aspire tests' -c user.email=tests@example.com -c commit.gpgsign=false commit -m 'Add template'",
                counter);
        }

        var source = repository ? "--template-repository \"file://$PWD/template\"" : "--template-path template";
        await auto.TypeAsync($"aspire new {source} --name PromptDemo --output out --suppress-agent-init --nologo");
        await auto.EnterAsync();

        await auto.WaitUntilTextAsync("Greeting text", timeout: TimeSpan.FromSeconds(60));
        await auto.TypeAsync("Hello from the terminal");
        await auto.EnterAsync();

        await auto.WaitUntilTextAsync("Include readme", timeout: TimeSpan.FromSeconds(30));
        if (!repository)
        {
            // Confirmation accepts y/n immediately; a trailing Enter would answer the next prompt.
            await auto.TypeAsync("n");
        }
        else
        {
            await auto.EnterAsync();
        }

        await auto.WaitUntilTextAsync("> HTTP API", timeout: TimeSpan.FromSeconds(30));
        if (repository)
        {
            await auto.DownAsync();
        }
        await auto.EnterAsync();

        await auto.WaitUntilTextAsync("Included features", timeout: TimeSpan.FromSeconds(30));
        await auto.WaitUntilTextAsync("> [X] Web support", timeout: TimeSpan.FromSeconds(30));
        await auto.DownAsync();
        await auto.TypeAsync(" ");
        await auto.EnterAsync();
        await auto.WaitForSuccessPromptAsync(counter);
        await auto.RunCommandAsync("! command -v dotnet", counter);

        var outputDirectory = Path.Combine(workspace.WorkspaceRoot.FullName, "out");
        var files = Directory.GetFiles(outputDirectory, "*", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToDictionary(path => Path.GetRelativePath(outputDirectory, path).Replace('\\', '/'), File.ReadAllText);
        await VerifyXunit.Verifier.Verify(files).UseDirectory(Path.Combine(testDirectory, "Snapshots"));
    }
}
