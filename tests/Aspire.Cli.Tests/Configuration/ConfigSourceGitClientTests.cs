// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Resources;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Tests.Configuration;

public class ConfigSourceGitClientTests(ITestOutputHelper outputHelper)
{
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

        var startInfo = client.CreateStartInfo(workspace.Path, ["clone", "--bare", "--", "https://example.com/configuration", "repository.git"]);

        Assert.Equal(expectedInteractive ? "1" : "0", startInfo.Environment["GIT_TERMINAL_PROMPT"]);
        Assert.Equal(!expectedInteractive, startInfo.CreateNoWindow);
        Assert.False(startInfo.RedirectStandardInput);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
    }

    [Theory]
    [InlineData("aspire.config.json")]
    [InlineData("teams/platform config.json")]
    public async Task Acquire_ReadsDefaultBranchSnapshotWithoutCheckingOut(string path)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, path, "{\"features\":{\"test\":true},\"custom\":\"\u65e5\u672c\u8a9e\"}");
        var cache = workspace.CreateDirectory("cache");
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());

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
    [InlineData("missing-branch")]
    [InlineData("0000000000000000000000000000000000000000")]
    public async Task Acquire_UnavailableRefFailsWithoutFallingBack(string reference)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, AspireConfigFile.FileName, "{}");
        var client = new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment());

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
