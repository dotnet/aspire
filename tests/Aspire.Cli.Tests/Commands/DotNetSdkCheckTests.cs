// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Commands;

public class DotNetSdkCheckTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task CheckAsync_SkipsCheck_WhenNoAppHostFound()
    {
        // No apphost in settings — skip .NET SDK check entirely
        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100")
        };
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(null)
        };
        var languageDiscovery = new TestLanguageDiscovery();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckAsync_SkipsCheck_WhenNonDotNetAppHostFound()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.ts"));

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100")
        };
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(appHostFile)
        };
        var languageDiscovery = new TestLanguageDiscoveryWithPolyglot();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckAsync_RunsCheck_WhenDotNetAppHostFound()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "MyAppHost.csproj"));

        var sdkInstaller = new TestDotNetSdkInstaller();
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(appHostFile)
        };
        var languageDiscovery = new TestLanguageDiscoveryWithPolyglot();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Pass, results[0].Status);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFail_WhenDotNetAppHostFound_AndSdkNotInstalled()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "MyAppHost.csproj"));

        var sdkInstaller = new TestDotNetSdkInstaller
        {
            CheckAsyncCallback = _ => (false, null, "10.0.100")
        };
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(appHostFile)
        };
        var languageDiscovery = new TestLanguageDiscoveryWithPolyglot();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Single(results);
        Assert.Equal(EnvironmentCheckStatus.Fail, results[0].Status);
        Assert.Contains(".NET SDK not found", results[0].Message);
    }

    [Fact]
    public async Task CheckAsync_SkipsCheck_WhenNoSettingsFileExists()
    {
        // No settings.json — can't determine language, skip .NET check
        var sdkInstaller = new TestDotNetSdkInstaller();
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(null)
        };
        var languageDiscovery = new TestLanguageDiscoveryWithPolyglot();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        Assert.Empty(results);
    }

    [Fact]
    public async Task CheckAsync_SkipsCheck_WhenLanguageNotRecognized()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var appHostFile = new FileInfo(Path.Combine(workspace.WorkspaceRoot.FullName, "unknown.xyz"));

        var sdkInstaller = new TestDotNetSdkInstaller();
        var projectLocator = new TestProjectLocator
        {
            GetAppHostFromSettingsAsyncCallback = _ => Task.FromResult<FileInfo?>(appHostFile)
        };
        var languageDiscovery = new TestLanguageDiscoveryWithPolyglot();

        var check = new DotNetSdkCheck(
            sdkInstaller,
            projectLocator,
            languageDiscovery,
            NullLogger<DotNetSdkCheck>.Instance);

        var results = await check.CheckAsync().DefaultTimeout();

        // Unrecognized file — skip .NET check
        Assert.Empty(results);
    }

    /// <summary>
    /// Test implementation of ILanguageDiscovery that includes polyglot language support.
    /// </summary>
    private sealed class TestLanguageDiscoveryWithPolyglot : ILanguageDiscovery
    {
        private static readonly LanguageInfo[] s_allLanguages =
        [
            new LanguageInfo(
                LanguageId: new LanguageId(KnownLanguageId.CSharp),
                DisplayName: KnownLanguageId.CSharpDisplayName,
                PackageName: "",
                DetectionPatterns: ["*.csproj", "*.fsproj", "*.vbproj", "apphost.cs"],
                CodeGenerator: "",
                AppHostFileName: null),
            new LanguageInfo(
                LanguageId: new LanguageId(KnownLanguageId.TypeScript),
                DisplayName: "TypeScript (Node.js)",
                PackageName: "Aspire.Hosting.CodeGeneration.TypeScript",
                DetectionPatterns: ["apphost.ts"],
                CodeGenerator: "TypeScript",
                AppHostFileName: "apphost.ts"),
        ];

        public Task<IEnumerable<LanguageInfo>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<LanguageInfo>>(s_allLanguages);

        public Task<string?> GetPackageForLanguageAsync(LanguageId languageId, CancellationToken cancellationToken = default)
        {
            var language = s_allLanguages.FirstOrDefault(l =>
                string.Equals(l.LanguageId.Value, languageId.Value, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(language?.PackageName);
        }

        public Task<LanguageId?> DetectLanguageAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
        {
            foreach (var language in s_allLanguages)
            {
                foreach (var pattern in language.DetectionPatterns)
                {
                    if (pattern.StartsWith("*.", StringComparison.Ordinal))
                    {
                        var extension = pattern[1..];
                        if (directory.Exists && directory.EnumerateFiles().Any(f => f.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                        {
                            return Task.FromResult<LanguageId?>(language.LanguageId);
                        }
                    }
                    else
                    {
                        var filePath = Path.Combine(directory.FullName, pattern);
                        if (File.Exists(filePath))
                        {
                            return Task.FromResult<LanguageId?>(language.LanguageId);
                        }
                    }
                }
            }
            return Task.FromResult<LanguageId?>(null);
        }

        public LanguageInfo? GetLanguageById(LanguageId languageId) =>
            s_allLanguages.FirstOrDefault(l =>
                string.Equals(l.LanguageId.Value, languageId.Value, StringComparison.OrdinalIgnoreCase));

        public LanguageInfo? GetLanguageByFile(FileInfo file) =>
            s_allLanguages.FirstOrDefault(l =>
                l.DetectionPatterns.Any(p => MatchesPattern(file.Name, p)));

        private static bool MatchesPattern(string fileName, string pattern)
        {
            if (pattern.StartsWith("*.", StringComparison.Ordinal))
            {
                var extension = pattern[1..];
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }
            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
