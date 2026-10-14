// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Agents.AspireSkills;
using Aspire.Cli.Agents.Hooks;
using Aspire.Cli.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Cli.Tests.Agents;

public class TelemetryHookInstallerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task EnsureInstalledAsync_MaterializesBothScriptsUnderAspireHooksDirectory()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var installer = CreateInstaller(workspace, home);

        var scripts = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();

        var expectedDirectory = Path.Combine(home.FullName, ".aspire", "hooks");
        Assert.Equal(Path.Combine(expectedDirectory, "track-telemetry.sh"), scripts.ShellScriptPath);
        Assert.Equal(Path.Combine(expectedDirectory, "track-telemetry.ps1"), scripts.PowerShellScriptPath);
        Assert.True(File.Exists(scripts.ShellScriptPath));
        Assert.True(File.Exists(scripts.PowerShellScriptPath));
    }

    [Fact]
    public async Task EnsureInstalledAsync_MatchesBundledHooksAndMetadata()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var bundleDirectory = workspace.CreateDirectory("bundle");
        var bundleProvider = new EmbeddedAspireSkillsBundleProvider(
            new AspireSkillsBundleProvider(),
            NullLogger<EmbeddedAspireSkillsBundleProvider>.Instance);
        var bundle = await bundleProvider.CreateBundleAsync(bundleDirectory, CancellationToken.None).DefaultTimeout();
        Assert.NotNull(bundle);

        var scripts = await CreateInstaller(workspace, home).EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();
        var installedPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["track-telemetry.sh"] = scripts.ShellScriptPath,
            ["track-telemetry.ps1"] = scripts.PowerShellScriptPath
        };

        await using var manifestStream = File.OpenRead(Path.Combine(bundleDirectory.FullName, "skill-manifest.json"));
        using var manifest = await JsonDocument.ParseAsync(manifestStream).DefaultTimeout();
        await using var metadataStream = typeof(TelemetryHookInstaller).Assembly.GetManifestResourceStream("aspire-skills.metadata.json");
        Assert.NotNull(metadataStream);
        using var metadata = await JsonDocument.ParseAsync(metadataStream).DefaultTimeout();

        var bundledHooks = manifest.RootElement.GetProperty("hooks");
        var recordedHooks = metadata.RootElement.GetProperty("hooks");
        Assert.Equal(bundledHooks.GetProperty("commitSha").GetString(), recordedHooks.GetProperty("commitSha").GetString());

        var bundledFiles = bundledHooks.GetProperty("files").EnumerateObject().ToArray();
        var recordedFiles = recordedHooks.GetProperty("files");
        var expectedNames = bundledFiles.Select(file => file.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedNames, installedPaths.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(expectedNames, recordedFiles.EnumerateObject().Select(file => file.Name).Order(StringComparer.Ordinal));

        foreach (var file in bundledFiles)
        {
            var bundledPath = Path.Combine(bundleDirectory.FullName, "hooks", "scripts", file.Name);
            var bundledContent = await ReadLfNormalizedAsync(bundledPath).DefaultTimeout();
            var installedContent = await ReadLfNormalizedAsync(installedPaths[file.Name]).DefaultTimeout();
            Assert.Equal(bundledContent, installedContent);

            var installedHash = Convert.ToHexStringLower(SHA512.HashData(Encoding.UTF8.GetBytes(installedContent)));
            Assert.Equal(file.Value.GetString(), installedHash);
            Assert.Equal(file.Value.GetString(), recordedFiles.GetProperty(file.Name).GetString());
        }
    }

    [Fact]
    public async Task EnsureInstalledAsync_ShellScriptUsesLfEndingsAndNoBom()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var installer = CreateInstaller(workspace, home);

        var scripts = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();

        var bytes = await File.ReadAllBytesAsync(scripts.ShellScriptPath).DefaultTimeout();
        // A UTF-8 BOM (EF BB BF) before the shebang stops the kernel from honoring `#!`.
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.DoesNotContain((byte)'\r', bytes);

        var content = await File.ReadAllTextAsync(scripts.ShellScriptPath).DefaultTimeout();
        Assert.StartsWith("#!", content);
    }

    [Fact]
    public async Task EnsureInstalledAsync_IsIdempotent_WhenContentUnchanged()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var installer = CreateInstaller(workspace, home);

        var first = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();
        var firstShellContent = await File.ReadAllTextAsync(first.ShellScriptPath).DefaultTimeout();
        File.SetLastWriteTimeUtc(first.ShellScriptPath, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(first.PowerShellScriptPath, new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var shellTimestamp = File.GetLastWriteTimeUtc(first.ShellScriptPath);
        var powerShellTimestamp = File.GetLastWriteTimeUtc(first.PowerShellScriptPath);

        var second = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();
        var secondShellContent = await File.ReadAllTextAsync(second.ShellScriptPath).DefaultTimeout();

        Assert.Equal(first.ShellScriptPath, second.ShellScriptPath);
        Assert.Equal(firstShellContent, secondShellContent);
        Assert.Equal(shellTimestamp, File.GetLastWriteTimeUtc(second.ShellScriptPath));
        Assert.Equal(powerShellTimestamp, File.GetLastWriteTimeUtc(second.PowerShellScriptPath));
    }

    [Fact]
    public async Task EnsureInstalledAsync_RewritesScript_WhenExistingContentDiffers()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var installer = CreateInstaller(workspace, home);

        var hooksDirectory = Path.Combine(home.FullName, ".aspire", "hooks");
        Directory.CreateDirectory(hooksDirectory);
        var shellPath = Path.Combine(hooksDirectory, "track-telemetry.sh");
        await File.WriteAllTextAsync(shellPath, "stale-content").DefaultTimeout();

        var scripts = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();

        var content = await File.ReadAllTextAsync(scripts.ShellScriptPath).DefaultTimeout();
        Assert.NotEqual("stale-content", content);
        Assert.StartsWith("#!", content);
    }

    [Fact]
    public async Task EnsureInstalledAsync_SetsExecutableBit_OnNonWindows()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var installer = CreateInstaller(workspace, home);

        var scripts = await installer.EnsureInstalledAsync(CancellationToken.None).DefaultTimeout();

        var mode = File.GetUnixFileMode(scripts.ShellScriptPath);
        Assert.True(mode.HasFlag(UnixFileMode.UserExecute));
    }

    [Fact]
    public async Task EnsureInstalledAsync_CancellationDoesNotCreateHookFiles()
    {
        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateInstaller(workspace, home).EnsureInstalledAsync(cancellation.Token)).DefaultTimeout();

        Assert.False(Directory.Exists(Path.Combine(home.FullName, ".aspire", "hooks")));
    }

    [Fact]
    public async Task EnsureInstalledAsync_LockedExistingScriptIsNotBlindlyOverwritten()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows file sharing is required.");
        }

        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var directory = Directory.CreateDirectory(Path.Combine(home.FullName, ".aspire", "hooks"));
        var path = Path.Combine(directory.FullName, "track-telemetry.sh");
        await File.WriteAllTextAsync(path, "existing").DefaultTimeout();
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            await Assert.ThrowsAnyAsync<IOException>(() =>
                CreateInstaller(workspace, home).EnsureInstalledAsync(CancellationToken.None)).DefaultTimeout();
        }

        Assert.Equal("existing", await File.ReadAllTextAsync(path).DefaultTimeout());
        Assert.Equal([path], Directory.EnumerateFiles(directory.FullName));
    }

    [Fact]
    public async Task EnsureInstalledAsync_FailedReplacementCleansItsStagingFile()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("Windows read-only replacement behavior is required.");
        }

        using var workspace = TemporaryWorkspace.CreateForCli(outputHelper);
        var home = workspace.CreateDirectory("home");
        var directory = Directory.CreateDirectory(Path.Combine(home.FullName, ".aspire", "hooks"));
        var path = Path.Combine(directory.FullName, "track-telemetry.sh");
        await File.WriteAllTextAsync(path, "existing").DefaultTimeout();
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            var error = await Record.ExceptionAsync(() =>
                CreateInstaller(workspace, home).EnsureInstalledAsync(CancellationToken.None)).DefaultTimeout();
            Assert.True(error is IOException or UnauthorizedAccessException);
            Assert.Equal("existing", await File.ReadAllTextAsync(path).DefaultTimeout());
            Assert.Equal([path], Directory.EnumerateFiles(directory.FullName));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    private static async Task<string> ReadLfNormalizedAsync(string path)
    {
        var content = await File.ReadAllTextAsync(path).DefaultTimeout();
        return content.ReplaceLineEndings("\n");
    }

    private static TelemetryHookInstaller CreateInstaller(TemporaryWorkspace workspace, DirectoryInfo home)
    {
        var executionContext = TestExecutionContextHelper.CreateExecutionContext(workspace.WorkspaceRoot, homeDirectory: home);
        return new TelemetryHookInstaller(executionContext, NullLogger<TelemetryHookInstaller>.Instance);
    }
}
