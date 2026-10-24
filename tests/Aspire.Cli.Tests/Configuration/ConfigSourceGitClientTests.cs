// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Cli.Configuration;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Configuration;

public class ConfigSourceGitClientTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RunProcess_DescendantHoldingOutputCannotBlockShutdown(bool parentExits)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var pidFile = Path.Combine(workspace.Path, "child.pid");
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "/bin/sh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-Command");
            // The child inherits both output pipes. Either leave its parent running for
            // cancellation, or exit the parent to exercise the bounded post-exit drain.
            startInfo.ArgumentList.Add(
                "$child = Start-Process powershell.exe -NoNewWindow -PassThru -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 60'; " +
                $"[IO.File]::WriteAllText('{pidFile.Replace("'", "''")}', [string]$child.Id); " +
                (parentExits ? "exit 0" : "Start-Sleep -Seconds 60"));
        }
        else
        {
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add("sleep 60 & echo $! > \"$1\"; " + (parentExits ? "exit 0" : "wait"));
            startInfo.ArgumentList.Add("test-parent");
            startInfo.ArgumentList.Add(pidFile);
        }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(30));
        var run = ConfigSourceGitClient.RunProcessAsync(startInfo, cancellation.Token);
        Process? child = null;
        try
        {
            while (child is null)
            {
                if (File.Exists(pidFile) && int.TryParse(await File.ReadAllTextAsync(pidFile, cancellation.Token), out var pid))
                {
                    child = Process.GetProcessById(pid);
                }
                else
                {
                    await Task.Delay(20, cancellation.Token);
                }
            }

            if (parentExits)
            {
                var error = await Assert.ThrowsAsync<ConfigSourceException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(ConfigSourceStrings.RepositoryUnavailable, error.Message);
            }
            else
            {
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                Assert.True(child.HasExited);
            }
        }
        finally
        {
            cancellation.Cancel();
            if (child is not null)
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                }
                child.Dispose();
            }
            await Record.ExceptionAsync(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
        }
    }

    [Fact]
    public void CreateStartInfo_AdvisoryFetchNeverOwnsTerminal()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var client = new ConfigSourceGitClient(TestHelpers.CreateInteractiveHostEnvironment());

        var startInfo = client.CreateStartInfo(workspace.Path, ["fetch"], allowInteractive: false);

        Assert.Equal("0", startInfo.Environment["GIT_TERMINAL_PROMPT"]);
        Assert.Equal("never", startInfo.Environment["GCM_INTERACTIVE"]);
        Assert.True(startInfo.CreateNoWindow);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.False(startInfo.RedirectStandardInput);
    }

    [Theory]
    [InlineData(false, null, null, true)]
    [InlineData(true, null, null, false)]
    [InlineData(false, "true", null, false)]
    [InlineData(false, null, "true", false)]
    public void CreateStartInfo_CredentialPromptsFollowHostInteractivity(
        bool nonInteractive, string? configuredNonInteractive, string? ci, bool expectedInteractive)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        using var configuration = new ConfigurationManager();
        configuration["ASPIRE_NON_INTERACTIVE"] = configuredNonInteractive;
        configuration["CI"] = ci;
        var hostEnvironment = new CliHostEnvironment(configuration, nonInteractive, isOutputRedirected: true);
        var client = new ConfigSourceGitClient(hostEnvironment);

        var startInfo = client.CreateStartInfo(workspace.Path, ["fetch", "--depth", "1", "--", "https://example.com/configuration", "HEAD"]);

        Assert.Equal(expectedInteractive ? "1" : "0", startInfo.Environment["GIT_TERMINAL_PROMPT"]);
        Assert.Equal(!expectedInteractive, startInfo.CreateNoWindow);
        Assert.False(startInfo.RedirectStandardInput);
        Assert.Equal(!expectedInteractive, startInfo.RedirectStandardOutput);
        Assert.Equal(!expectedInteractive, startInfo.RedirectStandardError);
        Assert.Equal(!expectedInteractive, startInfo.StandardOutputEncoding is not null);
        Assert.Equal(!expectedInteractive, startInfo.StandardErrorEncoding is not null);
        if (!expectedInteractive)
        {
            Assert.Equal("never", startInfo.Environment["GCM_INTERACTIVE"]);
        }
    }

    [Theory]
    [InlineData("init")]
    [InlineData("rev-parse")]
    [InlineData("ls-tree")]
    [InlineData("cat-file")]
    public void CreateStartInfo_LocalQueriesRemainCapturedWhenInteractive(string command)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var client = new ConfigSourceGitClient(TestHelpers.CreateInteractiveHostEnvironment());

        var startInfo = client.CreateStartInfo(workspace.Path, [command]);

        Assert.False(startInfo.RedirectStandardInput);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Equal(System.Text.Encoding.UTF8, startInfo.StandardOutputEncoding);
        Assert.Equal(System.Text.Encoding.UTF8, startInfo.StandardErrorEncoding);
    }

    [Theory]
    [InlineData("aspire.config.json", false)]
    [InlineData("aspire.config.json", true)]
    [InlineData("teams/platform config.json", false)]
    [InlineData("teams/platform config.json", true)]
    public async Task Acquire_ReadsDefaultBranchSnapshotWithoutCheckingOut(string path, bool interactive)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, path, "{\"features\":{\"test\":true},\"custom\":\"\u65e5\u672c\u8a9e\"}");
        var cache = workspace.CreateDirectory("cache");
        var client = new ConfigSourceGitClient(interactive
            ? TestHelpers.CreateInteractiveHostEnvironment()
            : TestHelpers.CreateNonInteractiveHostEnvironment());

        // Source-address validation is covered separately. A local transport exercises
        // real Git snapshot acquisition without contacting a network repository.
        var (commit, version, configuration) = await client.AcquireAsync(
            new() { Repository = workspace.Path, Path = path }, cache.FullName, TestContext.Current.CancellationToken);

        Assert.Matches("^[a-f0-9]{40,64}$", commit);
        Assert.Equal((await RunGitOutputAsync(workspace.Path, ["rev-parse", $"HEAD:{path}"])).Trim(), version);
        Assert.True(configuration["features"]!["test"]!.GetValue<bool>());
        Assert.Equal("\u65e5\u672c\u8a9e", configuration["custom"]!.GetValue<string>());
        Assert.False(File.Exists(Path.Combine(cache.FullName, path)));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("""{"features":{"test":"not-a-boolean"}}""")]
    [InlineData("""{"features:test":"not-a-boolean"}""")]
    [InlineData("""{"features":{"test":true},"features":{"test":false}}""")]
    [InlineData("""{"configSource:repository":"https://example.com/nested"}""")]
    [InlineData("""{"configSource":{"repository":"https://example.com/nested","path":"config.json"}}""")]
    public async Task Acquire_RejectsInvalidConfiguration(string content)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, content);

        await Assert.ThrowsAsync<ConfigSourceException>(() => new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment()).AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName }, workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Acquire_DistinguishesMissingFile()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, "other.json", "{}");

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() => new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment()).AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName }, workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));

        Assert.Equal(ConfigSourceStrings.MissingFile, error.Message);
    }

    [Theory]
    [InlineData(4 * 1024 * 1024, true)]
    [InlineData(4 * 1024 * 1024 + 1, false)]
    public async Task Acquire_EnforcesExactConfigurationSizeLimit(int bytes, bool valid)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, "{}".PadRight(bytes));
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());
        var cache = workspace.CreateDirectory("cache");
        var source = new ConfigSource { Repository = workspace.Path, Path = AspireConfigFile.FileName };

        if (valid)
        {
            var (_, _, configuration) = await client.AcquireAsync(source, cache.FullName, TestContext.Current.CancellationToken);
            Assert.Empty(configuration);
        }
        else
        {
            var error = await Assert.ThrowsAsync<ConfigSourceException>(() =>
                client.AcquireAsync(source, cache.FullName, TestContext.Current.CancellationToken));
            Assert.Equal(ConfigSourceStrings.InvalidConfiguration, error.Message);
        }
    }

    [Theory]
    [InlineData("config/preview")]
    [InlineData("refs/heads/config/preview")]
    public async Task Acquire_ReadsSelectedBranchInsteadOfDefaultBranch(string reference)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, """{"custom":"default"}""");
        var originalBranch = (await RunGitOutputAsync(workspace.Path, ["branch", "--show-current"])).Trim();
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "checkout", "-b", "config/preview");
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName), """{"custom":"preview"}""");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-am", "Preview configuration");
        var expectedCommit = (await RunGitOutputAsync(workspace.Path, ["rev-parse", "HEAD"])).Trim();
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "checkout", originalBranch);
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());

        var (commit, _, configuration) = await client.AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName, Ref = reference },
            workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCommit, commit);
        Assert.Equal("preview", configuration["custom"]!.GetValue<string>());
    }

    [Fact]
    public async Task Acquire_ReadsPinnedCommitOutsideShallowDefaultBranchTip()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, """{"custom":"pinned"}""");
        var expectedCommit = (await RunGitOutputAsync(workspace.Path, ["rev-parse", "HEAD"])).Trim();
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName), """{"custom":"latest"}""");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-am", "New configuration");
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());

        var (commit, _, configuration) = await client.AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName, Ref = expectedCommit },
            workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken);

        Assert.Equal(expectedCommit, commit);
        Assert.Equal("pinned", configuration["custom"]!.GetValue<string>());
    }

    [Fact]
    public async Task Check_IdentifiesFileChangesWithoutMaterializingJsonAndHonorsPins()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, "{}");
        var source = new ConfigSource { Repository = workspace.Path, Path = AspireConfigFile.FileName };
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());
        var original = await client.CheckAsync(source, workspace.CreateDirectory("first-check").FullName, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "unrelated.txt"), "Unrelated change");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "add", "unrelated.txt");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-m", "Unrelated commit");

        var unchanged = await client.CheckAsync(source, workspace.CreateDirectory("unchanged-check").FullName, TestContext.Current.CancellationToken);

        Assert.NotEqual(original.Commit, unchanged.Commit);
        Assert.Equal(original.Version, unchanged.Version);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName), """{"custom":"changed"}""");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-am", "Change configuration");
        var directory = workspace.CreateDirectory("changed-check");
        var changed = await client.CheckAsync(source, directory.FullName, TestContext.Current.CancellationToken);
        var pinned = await client.CheckAsync(new() { Repository = source.Repository, Path = source.Path, Ref = original.Commit },
            workspace.CreateDirectory("pinned-check").FullName, TestContext.Current.CancellationToken);

        Assert.NotEqual(original.Version, changed.Version);
        Assert.Equal(original, pinned);
        Assert.Equal(["repository.git"], directory.EnumerateFileSystemInfos().Select(p => p.Name));
    }

    [Theory]
    [InlineData("missing-branch", false)]
    [InlineData("missing-branch", true)]
    [InlineData("0000000000000000000000000000000000000000", false)]
    [InlineData("0000000000000000000000000000000000000000", true)]
    public async Task Acquire_UnavailableRefFailsWithoutFallingBack(string reference, bool interactive)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, "{}");
        var client = new ConfigSourceGitClient(interactive
            ? TestHelpers.CreateInteractiveHostEnvironment()
            : TestHelpers.CreateNonInteractiveHostEnvironment());

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() => client.AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName, Ref = reference },
            workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));

        Assert.Equal(ConfigSourceStrings.RepositoryUnavailable, error.Message);
    }

    [Fact]
    public async Task Acquire_RejectsSymlinkConfiguration()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, "target.json", "{}");
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, AspireConfigFile.FileName), "target.json");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "add", AspireConfigFile.FileName);
        // Write a symlink-mode tree entry directly so the test does not require Windows
        // developer mode or elevated filesystem symlink privileges.
        var entry = await RunGitOutputAsync(workspace.Path, ["ls-files", "--stage", AspireConfigFile.FileName]);
        var objectId = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "update-index", "--cacheinfo", $"120000,{objectId},{AspireConfigFile.FileName}");
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-m", "Add symlink configuration");

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() => new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment()).AcquireAsync(
            new() { Repository = workspace.Path, Path = AspireConfigFile.FileName }, workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));
        Assert.Equal(ConfigSourceStrings.MissingFile, error.Message);
    }

    private async Task CreateRepositoryAsync(TemporaryWorkspace workspace, string path, string json)
    {
        await GitTestHelper.EnsureGitAvailableAsync(outputHelper);
        await workspace.InitializeGitAsync();
        await GitTestHelper.ConfigureGitIdentityAsync(workspace.Path, outputHelper);
        var configPath = Path.Combine(workspace.Path, path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await File.WriteAllTextAsync(configPath, json);
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "add", "--", path);
        await GitTestHelper.RunGitAsync(workspace.Path, outputHelper, "commit", "-m", "Add configuration");
    }

    private static async Task<string> RunGitOutputAsync(string directory, string[] arguments)
    {
        var result = await System.Diagnostics.Process.RunAndCaptureTextAsync(new("git", arguments)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, TestContext.Current.CancellationToken);
        Assert.Equal(0, result.ExitStatus.ExitCode);
        return result.StandardOutput;
    }
}
