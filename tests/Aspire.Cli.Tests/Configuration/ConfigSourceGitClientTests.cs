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
        var (commit, configuration) = await client.AcquireAsync(new(workspace.Path, path), cache.FullName, TestContext.Current.CancellationToken);

        Assert.Matches("^[a-f0-9]{40,64}$", commit);
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
            new(workspace.Path, AspireConfigFile.FileName), workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Acquire_DistinguishesMissingFile()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        await CreateRepositoryAsync(workspace, "other.json", "{}");

        var error = await Assert.ThrowsAsync<ConfigSourceException>(() => new ConfigSourceGitClient(TestHelpers.CreateNonInteractiveHostEnvironment()).AcquireAsync(
            new(workspace.Path, AspireConfigFile.FileName), workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));

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
        var source = new ConfigSource(workspace.Path, AspireConfigFile.FileName);

        if (valid)
        {
            var (_, configuration) = await client.AcquireAsync(source, cache.FullName, TestContext.Current.CancellationToken);
            Assert.Empty(configuration);
        }
        else
        {
            var error = await Assert.ThrowsAsync<ConfigSourceException>(() =>
                client.AcquireAsync(source, cache.FullName, TestContext.Current.CancellationToken));
            Assert.Equal(ConfigSourceStrings.InvalidConfiguration, error.Message);
        }
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
            new(workspace.Path, AspireConfigFile.FileName), workspace.CreateDirectory("cache").FullName, TestContext.Current.CancellationToken));
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
