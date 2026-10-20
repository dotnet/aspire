// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Cli.Tests.Commands;

public sealed class GoAppHostToolingCheckTests(ITestOutputHelper outputHelper)
{
    private const string GoInstallLink = "https://go.dev/doc/install";

    private static readonly LanguageInfo s_goLanguage = new(
        LanguageId: new LanguageId(KnownLanguageId.Go),
        DisplayName: KnownLanguageId.GoDisplayName,
        PackageName: "Aspire.Hosting.CodeGeneration.Go",
        DetectionPatterns: ["apphost.go"],
        CodeGenerator: "Go",
        AppHostFileName: "apphost.go");

    [Theory]
    [InlineData("go version go1.26.1 windows/amd64", "1.26.1")]
    [InlineData("go version go1.27.0 darwin/arm64", "1.27.0")]
    [InlineData("go version go1.26rc1 linux/amd64", "1.26")]
    [InlineData("go version devel go1.27-4d2f8e1a5b Tue Jan 6 10:00:00 2026 +0000 linux/amd64", "1.27")]
    public async Task CheckAsync_ReturnsPass_WhenGoMeetsMinimumVersion(string versionOutput, string expectedVersion)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        string? versionResolverPath = null;

        var check = CreateCheck(
            workspace,
            appHostFile,
            commandResolver: command => $"/usr/bin/{command}",
            versionResolver: (path, _) =>
            {
                versionResolverPath = path;
                return Task.FromResult<string?>(versionOutput);
            });

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        Assert.Equal(EnvironmentCheckStatus.Pass, result.Status);
        Assert.Equal(GoAppHostToolingCheck.ToolsCheckName, result.Name);
        Assert.Equal(EnvironmentCheckCategories.Environment, result.Category);
        Assert.Equal($"Go AppHost tooling found (go {expectedVersion}).", result.Message);
        Assert.Equal("/usr/bin/go", versionResolverPath);
        Assert.NotNull(result.Metadata);
        Assert.Equal(KnownLanguageId.Go, result.Metadata["language"]?.GetValue<string>());
        Assert.Equal(expectedVersion, result.Metadata["version"]?.GetValue<string>());
        Assert.Equal(appHostFile.FullName, result.Metadata["appHostPath"]?.GetValue<string>());
    }

    [Fact]
    public async Task CheckAsync_ReturnsPass_WhenGoAppHostIsDiscoveredInWorkingDirectory()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);

        var check = CreateCheck(workspace, appHostFile: null);

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        Assert.Equal(EnvironmentCheckStatus.Pass, result.Status);
        Assert.NotNull(result.Metadata);
        Assert.Equal(appHostFile.FullName, result.Metadata["appHostPath"]?.GetValue<string>());
    }

    [Fact]
    public async Task CheckAsync_Skips_WhenNoGoAppHostExists()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var check = CreateCheck(workspace, appHostFile: null, commandResolver: _ => null);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckAsync_Skips_WhenAppHostIsNotGo()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs");
        File.WriteAllText(appHostPath, "// apphost");

        var check = CreateCheck(workspace, new FileInfo(appHostPath), commandResolver: _ => null);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFail_WhenGoIsMissing()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(workspace, appHostFile, commandResolver: _ => null);

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        Assert.Equal(EnvironmentCheckStatus.Fail, result.Status);
        Assert.Equal(GoAppHostToolingCheck.GoCommandCheckName, result.Name);
        Assert.Equal(EnvironmentCheckCategories.Environment, result.Category);
        Assert.Equal("Go AppHost requires 'go'.", result.Message);
        Assert.Equal("go is not installed or not found in PATH. Please install Go and try again.", result.Details);
        Assert.Equal("Install Go and rerun 'aspire doctor'.", result.Fix);
        Assert.Equal(GoInstallLink, result.Link);
        Assert.NotNull(result.Metadata);
        Assert.Equal(KnownLanguageId.Go, result.Metadata["language"]?.GetValue<string>());
    }

    [Fact]
    public async Task CheckAsync_ReturnsWarning_WhenGoIsOlderAndCanDownloadToolchain()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(workspace, appHostFile, versionResolver: ReturnsVersion("go version go1.24.1 windows/amd64"));

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        AssertVersionResult(result, EnvironmentCheckStatus.Warning);
        Assert.Equal("Go 1.24.1 is installed. Go downloads the Go 1.26 toolchain the first time the AppHost runs, which needs network access.", result.Details);
        Assert.Equal("Install Go 1.26 or later to avoid the download, then rerun 'aspire doctor'.", result.Fix);
        Assert.NotNull(result.Metadata);
        Assert.Equal("1.24.1", result.Metadata["version"]?.GetValue<string>());
    }

    [Fact]
    public async Task CheckAsync_ReturnsFail_WhenGoIsOlderAndToolchainIsLocal()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(
            workspace,
            appHostFile,
            versionResolver: ReturnsVersion("go version go1.24.1 windows/amd64"),
            environment: new Dictionary<string, string?> { ["GOTOOLCHAIN"] = "local" });

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        AssertVersionResult(result, EnvironmentCheckStatus.Fail);
        Assert.Equal("Go 1.24.1 is installed, and GOTOOLCHAIN=local prevents Go from downloading a newer toolchain.", result.Details);
        Assert.Equal("Install Go 1.26 or later, or unset GOTOOLCHAIN, then rerun 'aspire doctor'.", result.Fix);
    }

    [Fact]
    public async Task CheckAsync_ReturnsWarning_WhenToolchainIsNotLocal()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(
            workspace,
            appHostFile,
            versionResolver: ReturnsVersion("go version go1.24.1 windows/amd64"),
            environment: new Dictionary<string, string?> { ["GOTOOLCHAIN"] = "auto" });

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        AssertVersionResult(result, EnvironmentCheckStatus.Warning);
        Assert.Equal("Go 1.24.1 is installed. Go downloads the Go 1.26 toolchain the first time the AppHost runs, which needs network access.", result.Details);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFail_WhenGoPredatesToolchainSwitching()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(workspace, appHostFile, versionResolver: ReturnsVersion("go version go1.20.14 linux/amd64"));

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        AssertVersionResult(result, EnvironmentCheckStatus.Fail);
        Assert.Equal("Go 1.20.14 is installed, and Go versions before 1.21 cannot download a newer toolchain.", result.Details);
        Assert.Equal("Install Go 1.26 or later and rerun 'aspire doctor'.", result.Fix);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("go: command output without a version")]
    public async Task CheckAsync_ReturnsWarning_WhenGoVersionCannotBeDetermined(string? versionOutput)
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var appHostFile = CreateGoAppHost(workspace);
        var check = CreateCheck(workspace, appHostFile, versionResolver: ReturnsVersion(versionOutput));

        var result = Assert.Single(await check.CheckAsync().DefaultTimeout());

        AssertVersionResult(result, EnvironmentCheckStatus.Warning);
        Assert.Equal("The installed Go version could not be determined.", result.Details);
        Assert.Equal("Check that 'go version' prints a version and rerun 'aspire doctor'.", result.Fix);
        Assert.NotNull(result.Metadata);
        Assert.Null(result.Metadata["version"]);
    }

    [Theory]
    [InlineData("go version go1.26.1 windows/amd64", "1.26.1")]
    [InlineData("go version go1.26 linux/amd64", "1.26")]
    [InlineData("go version go1.26rc1 linux/amd64", "1.26")]
    [InlineData("go version go1.27beta2 darwin/arm64", "1.27")]
    [InlineData("go version devel go1.27-4d2f8e1a5b Tue Jan 6 10:00:00 2026 +0000 linux/amd64", "1.27")]
    [InlineData("go version go1.21.13 X:boringcrypto linux/amd64\n", "1.21.13")]
    public void TryParseGoVersion_ParsesVersionOutput(string output, string expectedVersion)
    {
        Assert.True(GoAppHostToolingCheck.TryParseGoVersion(output, out var version));
        Assert.Equal(expectedVersion, version.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("go version")]
    [InlineData("go version go1 linux/amd64")]
    [InlineData("'go' is not recognized as an internal or external command")]
    public void TryParseGoVersion_ReturnsFalse_WhenOutputHasNoVersion(string? output)
    {
        Assert.False(GoAppHostToolingCheck.TryParseGoVersion(output, out var version));
        Assert.Null(version);
    }

    [Theory]
    [InlineData("AtsGoCodeGenerator.cs")]
    [InlineData("GoLanguageSupport.cs")]
    public void MinimumGoVersion_MatchesGoModTemplate(string sourceFileName)
    {
        var sourcePath = Path.Combine(GetRepoRoot(), "src", "Aspire.Hosting.CodeGeneration.Go", sourceFileName);
        Assert.True(File.Exists(sourcePath), $"Expected the Go code generation source at {sourcePath}");

        // Both generators emit a go.mod whose directive sits on its own line: `go 1.26`.
        var directive = Regex.Match(File.ReadAllText(sourcePath), @"^\s*go (\d+\.\d+)\s*$", RegexOptions.Multiline);

        Assert.True(directive.Success, $"Expected a 'go <major>.<minor>' directive in the go.mod template of {sourceFileName}.");
        Assert.Equal(GoAppHostToolingCheck.MinimumGoVersion.ToString(), directive.Groups[1].Value);
    }

    private static FileInfo CreateGoAppHost(TemporaryWorkspace workspace)
    {
        var appHostPath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.go");
        File.WriteAllText(appHostPath, "package main");
        return new FileInfo(appHostPath);
    }

    private static GoAppHostToolingCheck CreateCheck(
        TemporaryWorkspace workspace,
        FileInfo? appHostFile,
        Func<string, string?>? commandResolver = null,
        Func<string, CancellationToken, Task<string?>>? versionResolver = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult(appHostFile)
        };

        return new GoAppHostToolingCheck(
            projectLocator,
            new TestLanguageDiscovery(s_goLanguage),
            CreateExecutionContext(workspace),
            new TestEnvironment(environment),
            commandResolver ?? (command => $"/usr/bin/{command}"),
            versionResolver ?? ReturnsVersion("go version go1.26.1 windows/amd64"));
    }

    private static Func<string, CancellationToken, Task<string?>> ReturnsVersion(string? versionOutput) =>
        (_, _) => Task.FromResult(versionOutput);

    private static void AssertVersionResult(EnvironmentCheckResult result, EnvironmentCheckStatus expectedStatus)
    {
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(GoAppHostToolingCheck.VersionCheckName, result.Name);
        Assert.Equal(EnvironmentCheckCategories.Environment, result.Category);
        Assert.Equal("Go AppHost requires Go 1.26 or later.", result.Message);
        Assert.Equal(GoInstallLink, result.Link);
    }

    private static CliExecutionContext CreateExecutionContext(TemporaryWorkspace workspace) =>
        new(
            workingDirectory: workspace.WorkspaceRoot,
            hivesDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire-hives"),
            cacheDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire-cache"),
            sdksDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire-sdks"),
            logsDirectory: workspace.WorkspaceRoot.CreateSubdirectory(".aspire-logs"),
            logFilePath: "test.log",
            identityChannel: "local");

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return dir.FullName;
    }
}
