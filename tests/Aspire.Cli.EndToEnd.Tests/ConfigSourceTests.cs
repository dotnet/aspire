// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aspire.Cli.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Cli.EndToEnd.Tests;

public sealed class ConfigSourceTests(ITestOutputHelper output)
{
    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task New_AppliesEnvironmentConfigurationSource()
        => RunScenarioAsync(Scenario.Create);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [CaptureWorkspaceOnFailure]
    public Task New_PromptsForConfigurationSourceConsent(bool accept)
        => RunScenarioAsync(accept ? Scenario.Accept : Scenario.Decline);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task New_NonInteractiveRequiresExplicitConfigurationSourceConsent()
        => RunScenarioAsync(Scenario.MissingConsent);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task New_GitAuthenticationPromptReceivesTerminalInput()
        => RunScenarioAsync(Scenario.Authenticate);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task Run_NotifiesWhenConfigurationSourceUpdateIsAvailable()
        => RunScenarioAsync(Scenario.Notify);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task Update_ConfigSourceAppliesAvailableUpdate()
        => RunScenarioAsync(Scenario.Update);

    [Fact]
    [CaptureWorkspaceOnFailure]
    public Task Update_ConfigSourceReportsAlreadyUpToDate()
        => RunScenarioAsync(Scenario.Unchanged);

    private async Task RunScenarioAsync(Scenario scenario)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repoRoot = CliE2ETestHelpers.GetRepoRoot();
        var strategy = CliInstallStrategy.Detect(output.WriteLine);
        if (strategy.Mode is not (CliInstallMode.LocalHive or CliInstallMode.PullRequest or CliInstallMode.LocalArchive))
        {
            Assert.Skip("Configuration sources require a current source build. Run with ASPIRE_E2E_ARCHIVE or in pull request CI.");
        }

        using var workspace = TemporaryWorkspace.Create(output);
        var root = workspace.WorkspaceRoot.FullName;
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(root, "configuration source"));
        var sourceFile = Path.Combine(Directory.CreateDirectory(Path.Combine(sourceDirectory.FullName, "teams")).FullName, "platform.json");
        var configCopy = Path.Combine(root, "actual-config.json");
        var versionFile = Path.Combine(root, "source-version.txt");
        await WriteSourceAsync(sourceFile, "original", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(root, "check-source.cs"), """
            using System.Diagnostics;
            using System.Globalization;
            using System.Text.Json.Nodes;

            var home = Environment.GetEnvironmentVariable("ASPIRE_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspire");
            var check = Directory.EnumerateDirectories(Path.Combine(home, "configsources"))
                .Select(directory => Path.Combine(directory, "check.json"))
                .Where(File.Exists)
                .Single();
            if (args[0] == "expire")
            {
                var metadata = JsonNode.Parse(await File.ReadAllTextAsync(check))!.AsObject();
                metadata["checkedAt"] = "2000-01-01T00:00:00+00:00";
                await File.WriteAllTextAsync(check, metadata.ToJsonString());
                return;
            }

            if (args[0] != "wait")
            {
                throw new ArgumentException("Expected expire or wait.");
            }

            var workspace = Environment.GetEnvironmentVariable("ASPIRE_E2E_WORKSPACE")
                ?? throw new InvalidOperationException("ASPIRE_E2E_WORKSPACE is not set.");
            var expected = (await File.ReadAllTextAsync(Path.Combine(workspace, "source-version.txt"))).Trim();
            using var process = Process.GetProcessById(int.Parse(args[1], CultureInfo.InvariantCulture));
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromMinutes(2))
            {
                process.Refresh();
                if (process.HasExited)
                {
                    throw new InvalidOperationException("The AppHost command exited before completing its configuration source check.");
                }

                var metadata = JsonNode.Parse(await File.ReadAllTextAsync(check))!.AsObject();
                if (metadata["version"]!.GetValue<string>() == expected)
                {
                    return;
                }

                await Task.Delay(100);
            }

            throw new TimeoutException("The AppHost command did not complete its configuration source check.");
            """, cancellationToken);

        using var terminal = CliE2ETestHelpers.CreateDockerTestTerminal(repoRoot, strategy, output, workspace: workspace);
        var counter = new SequenceCounter();
        var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));
        await using var terminalRun = CliE2ETestHelpers.StartRun(terminal, workspace, auto, counter, output, cancellationToken);
        await auto.PrepareDockerEnvironmentAsync(counter, workspace);
        await auto.InstallAspireCliAsync(strategy, counter);
        await auto.RunCommandAsync("aspire config set features.updateNotificationsEnabled false -g", counter);
        // The bind-mounted directory belongs to the host runner, not the container's root user.
        await auto.RunCommandAsync("git config --global --add safe.directory \"$ASPIRE_E2E_WORKSPACE/configuration source\"", counter);
        await auto.RunCommandAsync("git -C 'configuration source' init --initial-branch=main", counter);
        await auto.RunCommandAsync("git -C 'configuration source' config user.name 'Aspire E2E'", counter);
        await auto.RunCommandAsync("git -C 'configuration source' config user.email aspire-e2e@example.com", counter);
        await CommitSourceAsync(auto, counter);
        await auto.RunCommandAsync(
            "export ASPIRE_INIT_SOURCE_URL=\"$ASPIRE_E2E_WORKSPACE/configuration source\" " +
            "ASPIRE_INIT_SOURCE_PATH=teams/platform.json ASPIRE_INIT_SOURCE_REF=main", counter);
        const string newCommand = "aspire new aspire-empty --language csharp --name SourceApp --output SourceApp " +
            "--suppress-agent-init --localhost-tld false";
        if (scenario is Scenario.MissingConsent)
        {
            await auto.RunCommandAsync(
                $"{newCommand} --non-interactive > \"$ASPIRE_E2E_WORKSPACE/consent.log\" 2>&1; " +
                $"result=$?; cat \"$ASPIRE_E2E_WORKSPACE/consent.log\"; test \"$result\" -eq {CliExitCodes.InvalidCommand}",
                counter, timeout: TimeSpan.FromMinutes(3));
            var consentOutput = await File.ReadAllTextAsync(Path.Combine(root, "consent.log"), cancellationToken);
            Assert.Contains("Specify --use-config-source true to accept it or --use-config-source false",
                Regex.Replace(consentOutput, @"\s+", " "));
            Assert.False(Directory.Exists(Path.Combine(root, "SourceApp")));
            await auto.RunCommandAsync("test ! -d \"$HOME/.aspire/configsources\"", counter);
            return;
        }

        if (scenario is Scenario.Accept or Scenario.Decline)
        {
            await auto.TypeAsync(newCommand);
            await auto.EnterAsync();
            var consentPrompt = new CellPatternSearcher().Find("Use configuration from this source?");
            await auto.WaitUntilAsync(snapshot => consentPrompt.Search(snapshot).Count > 0,
                timeout: TimeSpan.FromMinutes(2), description: "waiting for configuration source consent");
            if (scenario is Scenario.Decline)
            {
                await auto.DownAsync();
            }
            await auto.EnterAsync();
            await auto.WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromMinutes(3));
        }
        else if (scenario is Scenario.Authenticate)
        {
            await AuthenticateGitSourceAsync(auto, counter, root, cancellationToken);
            await auto.TypeAsync($"{newCommand} --use-config-source true");
            await auto.EnterAsync();
            var authenticationPrompt = new CellPatternSearcher().Find("Configuration source Git authentication code:");
            await auto.WaitUntilAsync(snapshot => authenticationPrompt.Search(snapshot).Count > 0,
                timeout: TimeSpan.FromMinutes(2), description: "waiting for the Git authentication prompt without a trailing newline");
            await auto.TypeAsync("allow-source");
            await auto.EnterAsync();
            var authenticated = new CellPatternSearcher().Find("Configuration source Git authentication accepted.");
            await auto.WaitUntilAsync(snapshot => authenticated.Search(snapshot).Count > 0,
                timeout: TimeSpan.FromSeconds(30), description: "waiting for Git to acknowledge terminal input");
            await auto.WaitForSuccessPromptAsync(counter, timeout: TimeSpan.FromMinutes(3));
            Assert.Equal("accepted", await File.ReadAllTextAsync(Path.Combine(root, "git-authenticated.txt"), cancellationToken));
        }
        else
        {
            await auto.RunCommandAsync($"{newCommand} --use-config-source true --non-interactive", counter,
                timeout: TimeSpan.FromMinutes(3));
        }

        await auto.RunCommandAsync("cd SourceApp", counter);
        await auto.RunCommandAsync("aspire config set docs.custom keep", counter);
        // Source materialization creates an owner-only file inside the container. Export its
        // contents without changing those permissions so the host can assert the saved values.
        await auto.RunCommandAsync("cat aspire.config.json > \"$ASPIRE_E2E_WORKSPACE/actual-config.json\"", counter);
        var original = JsonNode.Parse(await File.ReadAllTextAsync(configCopy, cancellationToken))!.AsObject();
        if (scenario is Scenario.Decline)
        {
            Assert.Null(original["configSource"]);
            Assert.Equal("""{"custom":"keep"}""", original["docs"]!.ToJsonString());
            await auto.RunCommandAsync("test ! -d \"$HOME/.aspire/configsources\"", counter);
            return;
        }

        var originalVersion = (await File.ReadAllTextAsync(versionFile, cancellationToken)).Trim();
        AssertSourceValues(original, "original", originalVersion);
        Assert.Equal(scenario is Scenario.Authenticate
                ? "ssh://git@config-source.example/config"
                : CliE2ETestHelpers.ToContainerPath(sourceDirectory.FullName, workspace),
            original["configSource"]!["repository"]!.GetValue<string>());
        Assert.Equal("teams/platform.json", original["configSource"]!["path"]!.GetValue<string>());
        Assert.Equal("main", original["configSource"]!["ref"]!.GetValue<string>());
        if (scenario is Scenario.Create or Scenario.Accept or Scenario.Authenticate)
        {
            return;
        }

        if (scenario is not Scenario.Unchanged)
        {
            await WriteSourceAsync(sourceFile, "updated", cancellationToken);
            await CommitSourceAsync(auto, counter);
            Assert.NotEqual(originalVersion, (await File.ReadAllTextAsync(versionFile, cancellationToken)).Trim());
        }

        if (scenario is Scenario.Notify)
        {
            // Creation records a fresh successful check. Expire only this isolated source's
            // observation, then wait for the new blob version before cancelling the command.
            // This avoids both the 24-hour cadence and a race with command-exit cancellation.
            await auto.RunCommandAsync("dotnet run \"$ASPIRE_E2E_WORKSPACE/check-source.cs\" -- expire", counter);
            await auto.RunCommandAsync(
                "aspire run --non-interactive > \"$ASPIRE_E2E_WORKSPACE/run.log\" 2>&1 </dev/null & source_run_pid=$!", counter);
            await auto.RunCommandAsync("dotnet run \"$ASPIRE_E2E_WORKSPACE/check-source.cs\" -- wait \"$source_run_pid\"", counter,
                timeout: TimeSpan.FromMinutes(3));
            await auto.RunCommandAsync("kill -INT \"$source_run_pid\"; timeout 120 tail --pid=\"$source_run_pid\" -f /dev/null; wait \"$source_run_pid\"", counter,
                timeout: TimeSpan.FromMinutes(3));
            await auto.RunCommandAsync("cat \"$ASPIRE_E2E_WORKSPACE/run.log\"", counter);
            Assert.Contains("A new version of this project's configuration source is available.",
                await File.ReadAllTextAsync(Path.Combine(root, "run.log"), cancellationToken));
        }
        else
        {
            await auto.RunCommandAsync(
                "set -o pipefail; aspire update --config-source --yes --non-interactive 2>&1 | tee \"$ASPIRE_E2E_WORKSPACE/update.log\"",
                counter, timeout: TimeSpan.FromMinutes(2));
            var message = scenario is Scenario.Unchanged
                ? "app configuration already up to date"
                : "app configuration updated";
            var updateOutput = await File.ReadAllTextAsync(Path.Combine(root, "update.log"), cancellationToken);
            Assert.Contains(message, Regex.Replace(updateOutput, @"\s+", " "));
        }

        await auto.RunCommandAsync("cat aspire.config.json > \"$ASPIRE_E2E_WORKSPACE/actual-config.json\"", counter);
        var actual = JsonNode.Parse(await File.ReadAllTextAsync(configCopy, cancellationToken))!.AsObject();
        if (scenario is Scenario.Update)
        {
            var version = (await File.ReadAllTextAsync(versionFile, cancellationToken)).Trim();
            AssertSourceValues(actual, "updated", version);
            original["docs"]!["llmsTxtUrl"] = actual["docs"]!["llmsTxtUrl"]!.DeepClone();
            original["docs"]!["api"]!["sitemapUrl"] = actual["docs"]!["api"]!["sitemapUrl"]!.DeepClone();
            original["configSource"]!["appliedVersion"] = version;
        }

        Assert.Equal(original.ToJsonString(), actual.ToJsonString());
    }

    private static async Task AuthenticateGitSourceAsync(
        Hex1bTerminalAutomator auto, SequenceCounter counter, string root, CancellationToken cancellationToken)
    {
        // Substitute only SSH authentication, not Git acquisition. The launcher challenges the
        // terminal, then serves the local repository with real git-upload-pack. No network or
        // real credentials are needed, and stdout remains exclusively Git's wire protocol.
        await File.WriteAllTextAsync(Path.Combine(root, "ssh-probe.cs"), """
            using System.Diagnostics;

            var workspace = Environment.GetEnvironmentVariable("ASPIRE_E2E_WORKSPACE")
                ?? throw new InvalidOperationException("ASPIRE_E2E_WORKSPACE is not set.");
            if (Environment.GetEnvironmentVariable("GIT_TERMINAL_PROMPT") != "1")
            {
                throw new InvalidOperationException("Git terminal authentication was not enabled.");
            }

            Console.Error.Write("Configuration source Git authentication code: ");
            Console.Error.Flush();
            // SSH's stdin carries Git protocol traffic. Like an SSH authentication prompt,
            // read the challenge response from the controlling terminal instead.
            using (var terminal = new StreamReader(new FileStream("/dev/tty", FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (await terminal.ReadLineAsync() != "allow-source")
                {
                    Console.Error.WriteLine("Configuration source Git authentication failed.");
                    return 1;
                }
            }

            Console.Error.WriteLine("Configuration source Git authentication accepted.");
            await File.WriteAllTextAsync(Path.Combine(workspace, "git-authenticated.txt"), "accepted");
            var startInfo = new ProcessStartInfo("git")
            {
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("upload-pack");
            startInfo.ArgumentList.Add(Path.Combine(workspace, "configuration source"));
            // A real SSH server does not inherit the client's repository environment.
            foreach (var name in new[] { "GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES" })
            {
                startInfo.Environment.Remove(name);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start git-upload-pack.");
            await process.WaitForExitAsync();
            return process.ExitCode;
            """, cancellationToken);
        await auto.RunCommandAsync(
            "dotnet publish \"$ASPIRE_E2E_WORKSPACE/ssh-probe.cs\" -p:PublishAot=false --output \"$ASPIRE_E2E_WORKSPACE/ssh-probe\"",
            counter, timeout: TimeSpan.FromMinutes(2));
        // Skip Git's SSH-variant discovery so the launcher is invoked only for the fetch.
        await auto.RunCommandAsync(
            "export GIT_SSH_VARIANT=ssh GIT_SSH_COMMAND=\"dotnet '$ASPIRE_E2E_WORKSPACE/ssh-probe/ssh-probe.dll'\" " +
            "ASPIRE_INIT_SOURCE_URL=ssh://git@config-source.example/config", counter);
    }

    private static Task CommitSourceAsync(Hex1bTerminalAutomator auto, SequenceCounter counter)
        => auto.RunCommandAsync(
            "git -C \"$ASPIRE_E2E_WORKSPACE/configuration source\" add teams/platform.json && " +
            "git -C \"$ASPIRE_E2E_WORKSPACE/configuration source\" -c commit.gpgsign=false commit -m 'Update configuration' && " +
            "git -C \"$ASPIRE_E2E_WORKSPACE/configuration source\" rev-parse HEAD:teams/platform.json > \"$ASPIRE_E2E_WORKSPACE/source-version.txt\"", counter);

    private static Task WriteSourceAsync(string path, string version, CancellationToken cancellationToken)
        => File.WriteAllTextAsync(path, $$"""
            {
              "docs": {
                "llmsTxtUrl": "https://docs.example/{{version}}/llms.txt",
                "api": {
                  "sitemapUrl": "https://docs.example/{{version}}/sitemap.xml"
                }
              }
            }
            """, cancellationToken);

    private static void AssertSourceValues(JsonObject config, string version, string appliedVersion)
    {
        Assert.Equal($"https://docs.example/{version}/llms.txt", config["docs"]!["llmsTxtUrl"]!.GetValue<string>());
        Assert.Equal($"https://docs.example/{version}/sitemap.xml", config["docs"]!["api"]!["sitemapUrl"]!.GetValue<string>());
        Assert.Equal("keep", config["docs"]!["custom"]!.GetValue<string>());
        Assert.Equal(appliedVersion, config["configSource"]!["appliedVersion"]!.GetValue<string>());
    }

    private enum Scenario
    {
        Create,
        Accept,
        Decline,
        MissingConsent,
        Authenticate,
        Notify,
        Update,
        Unchanged
    }
}
