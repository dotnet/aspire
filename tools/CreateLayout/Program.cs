// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Tools.CreateLayout;

/// <summary>
/// Creates the Aspire bundle layout for distribution.
/// This tool assembles all components into a self-contained package.
/// </summary>
/// <remarks>
/// See docs/specs/bundle.md for the complete bundle specification and layout structure.
/// </remarks>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output directory for the layout",
            Required = true
        };

        var artifactsOption = new Option<string>("--artifacts", "-a")
        {
            Description = "Path to build artifacts directory",
            Required = true
        };

        var ridOption = new Option<string>("--rid")
        {
            Description = "Runtime identifier",
            Required = true
        };

        var runtimeOption = new Option<string?>("--runtime", "-r")
        {
            Description = "Path to .NET runtime to include (alternative to --download-runtime)"
        };

        var bundleVersionOption = new Option<string>("--bundle-version")
        {
            Description = "Version string for the layout",
            DefaultValueFactory = _ => "0.0.0-dev"
        };

        var runtimeVersionOption = new Option<string>("--runtime-version")
        {
            Description = ".NET SDK version to download",
            Required = true
        };

        var downloadRuntimeOption = new Option<bool>("--download-runtime")
        {
            Description = "Download .NET and ASP.NET runtimes from Microsoft"
        };

        var archiveOption = new Option<bool>("--archive")
        {
            Description = "Create archive (zip/tar.gz) after building"
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        var rootCommand = new RootCommand("CreateLayout - Build Aspire bundle layout for distribution");
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(artifactsOption);
        rootCommand.Options.Add(ridOption);
        rootCommand.Options.Add(runtimeOption);
        rootCommand.Options.Add(bundleVersionOption);
        rootCommand.Options.Add(runtimeVersionOption);
        rootCommand.Options.Add(downloadRuntimeOption);
        rootCommand.Options.Add(archiveOption);
        rootCommand.Options.Add(verboseOption);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var outputPath = parseResult.GetValue(outputOption)!;
            var artifactsPath = parseResult.GetValue(artifactsOption)!;
            var rid = parseResult.GetValue(ridOption)!;
            var runtimePath = parseResult.GetValue(runtimeOption);
            var version = parseResult.GetValue(bundleVersionOption)!;
            var runtimeVersion = parseResult.GetValue(runtimeVersionOption)!;
            var downloadRuntime = parseResult.GetValue(downloadRuntimeOption);
            var createArchive = parseResult.GetValue(archiveOption);
            var verbose = parseResult.GetValue(verboseOption);

            try
            {
                using var builder = new LayoutBuilder(outputPath, artifactsPath, runtimePath, rid, version, runtimeVersion, downloadRuntime, verbose);
                await builder.BuildAsync().ConfigureAwait(false);

                if (createArchive)
                {
                    await builder.CreateArchiveAsync().ConfigureAwait(false);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        });

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

}

/// <summary>
/// Builds the layout directory structure.
/// </summary>
internal sealed class LayoutBuilder : IDisposable
{
    private readonly string _outputPath;
    private readonly string _artifactsPath;
    private readonly string? _runtimePath;
    private readonly string _rid;
    private readonly string _version;
    private readonly string _runtimeVersion;
    private readonly bool _downloadRuntime;
    private readonly bool _verbose;
    private readonly HttpClient _httpClient = new();

    public LayoutBuilder(string outputPath, string artifactsPath, string? runtimePath, string rid, string version, string runtimeVersion, bool downloadRuntime, bool verbose)
    {
        _outputPath = Path.GetFullPath(outputPath);
        _artifactsPath = Path.GetFullPath(artifactsPath);
        _runtimePath = runtimePath is not null ? Path.GetFullPath(runtimePath) : null;
        _rid = rid;
        _version = version;
        _runtimeVersion = runtimeVersion;
        _downloadRuntime = downloadRuntime;
        _verbose = verbose;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task BuildAsync()
    {
        Log($"Building layout for {_rid} version {_version}");
        Log($"Output: {_outputPath}");
        Log($"Artifacts: {_artifactsPath}");

        // Clean and create output directory
        if (Directory.Exists(_outputPath))
        {
            Directory.Delete(_outputPath, recursive: true);
        }
        Directory.CreateDirectory(_outputPath);

        // Copy components
        await CopyCliAsync().ConfigureAwait(false);
        await CopyRuntimeAsync().ConfigureAwait(false);
        await CopyNuGetHelperAsync().ConfigureAwait(false);
        await CopyAppHostServerAsync().ConfigureAwait(false);
        await CopyDashboardAsync().ConfigureAwait(false);
        await CopyDcpAsync().ConfigureAwait(false);

        // Enable rollforward for all managed tools
        EnableRollForwardForAllTools();

        Log("Layout build complete!");
    }

    private async Task CopyCliAsync()
    {
        Log("Copying CLI...");

        var cliPublishPath = FindPublishPath("Aspire.Cli");
        if (cliPublishPath is null)
        {
            throw new InvalidOperationException("CLI publish output not found. Run 'dotnet publish' on Aspire.Cli first.");
        }

        var cliExe = _rid.StartsWith("win", StringComparison.OrdinalIgnoreCase) ? "aspire.exe" : "aspire";
        var sourceExe = Path.Combine(cliPublishPath, cliExe);

        if (!File.Exists(sourceExe))
        {
            throw new InvalidOperationException($"CLI executable not found at {sourceExe}");
        }

        var destExe = Path.Combine(_outputPath, cliExe);
        File.Copy(sourceExe, destExe, overwrite: true);

        // Make executable on Unix
        if (!_rid.StartsWith("win", StringComparison.OrdinalIgnoreCase))
        {
            await SetExecutableAsync(destExe).ConfigureAwait(false);
        }

        Log($"  Copied {cliExe}");
    }

    private async Task CopyRuntimeAsync()
    {
        Log("Copying .NET runtime...");

        var runtimeDir = Path.Combine(_outputPath, "runtime");
        Directory.CreateDirectory(runtimeDir);

        if (_runtimePath is not null && Directory.Exists(_runtimePath))
        {
            CopyRuntimeFromPath(_runtimePath, runtimeDir);
            Log($"  Copied runtime from {_runtimePath}");
        }
        else if (_downloadRuntime)
        {
            // Download runtime from Microsoft
            await DownloadRuntimeAsync(runtimeDir).ConfigureAwait(false);
        }
        else
        {
            // Try to find runtime in artifacts or use shared runtime
            var sharedRuntime = FindSharedRuntime();
            if (sharedRuntime is not null)
            {
                CopyRuntimeFromPath(sharedRuntime, runtimeDir);
                Log($"  Copied shared runtime from {sharedRuntime}");
            }
            else
            {
                Log("  WARNING: No runtime found. Layout will require runtime to be downloaded separately.");
                Log("           Use --download-runtime to download the runtime from Microsoft.");
                await File.WriteAllTextAsync(
                    Path.Combine(runtimeDir, "README.txt"),
                    "Place .NET runtime files here.\n").ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Copy runtime from a source path, excluding unnecessary frameworks like WindowsDesktop.App.
    /// </summary>
    private void CopyRuntimeFromPath(string sourcePath, string destPath)
    {
        // Copy everything except the shared/Microsoft.WindowsDesktop.App directory
        var sharedDir = Path.Combine(sourcePath, "shared");
        if (Directory.Exists(sharedDir))
        {
            var destSharedDir = Path.Combine(destPath, "shared");
            Directory.CreateDirectory(destSharedDir);

            // Only copy NETCore.App and AspNetCore.App - skip WindowsDesktop.App to save space
            var frameworksToCopy = new[] { "Microsoft.NETCore.App", "Microsoft.AspNetCore.App" };
            foreach (var framework in frameworksToCopy)
            {
                var srcFrameworkDir = Path.Combine(sharedDir, framework);
                if (Directory.Exists(srcFrameworkDir))
                {
                    CopyDirectory(srcFrameworkDir, Path.Combine(destSharedDir, framework));
                }
            }
        }

        // Copy host directory
        var hostDir = Path.Combine(sourcePath, "host");
        if (Directory.Exists(hostDir))
        {
            CopyDirectory(hostDir, Path.Combine(destPath, "host"));
        }

        // Copy dotnet executable and related files
        var isWindows = _rid.StartsWith("win", StringComparison.OrdinalIgnoreCase);
        var dotnetExe = isWindows ? "dotnet.exe" : "dotnet";
        var dotnetPath = Path.Combine(sourcePath, dotnetExe);
        if (File.Exists(dotnetPath))
        {
            File.Copy(dotnetPath, Path.Combine(destPath, dotnetExe), overwrite: true);
        }

        // Copy LICENSE and ThirdPartyNotices if present
        foreach (var file in new[] { "LICENSE.txt", "ThirdPartyNotices.txt" })
        {
            var srcFile = Path.Combine(sourcePath, file);
            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, Path.Combine(destPath, file), overwrite: true);
            }
        }
    }

    private async Task DownloadRuntimeAsync(string runtimeDir)
    {
        Log($"  Downloading .NET SDK {_runtimeVersion} for {_rid}...");

        var isWindows = _rid.StartsWith("win", StringComparison.OrdinalIgnoreCase);
        var archiveExt = isWindows ? "zip" : "tar.gz";

        // Download the full SDK - it contains runtime, aspnetcore, and dev-certs tool
        var sdkUrl = $"https://builds.dotnet.microsoft.com/dotnet/Sdk/{_runtimeVersion}/dotnet-sdk-{_runtimeVersion}-{_rid}.{archiveExt}";
        await DownloadAndExtractSdkAsync(sdkUrl, runtimeDir).ConfigureAwait(false);

        Log($"  SDK components extracted successfully");
    }

    private async Task DownloadAndExtractSdkAsync(string url, string runtimeDir)
    {
        Log($"    Downloading SDK from {url}...");

        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire-sdk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var isWindows = _rid.StartsWith("win", StringComparison.OrdinalIgnoreCase);
            var archiveExt = isWindows ? "zip" : "tar.gz";
            var archivePath = Path.Combine(tempDir, $"sdk.{archiveExt}");

            // Download the archive
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = File.Create(archivePath);
                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            Log($"    Extracting SDK...");

            // Extract the archive
            var extractDir = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractDir);

            if (isWindows)
            {
                ZipFile.ExtractToDirectory(archivePath, extractDir);
            }
            else
            {
                // Use tar to extract on Unix
                var psi = new ProcessStartInfo
                {
                    FileName = "tar",
                    Arguments = $"-xzf \"{archivePath}\" -C \"{extractDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var process = Process.Start(psi);
                await process!.WaitForExitAsync().ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to extract SDK: tar exited with code {process.ExitCode}");
                }
            }

            // Extract runtime components: shared/, host/, dotnet executable
            Log($"    Extracting runtime components...");

            // Copy only the shared frameworks we need (exclude WindowsDesktop.App to save space)
            var sharedDir = Path.Combine(extractDir, "shared");
            if (Directory.Exists(sharedDir))
            {
                var destSharedDir = Path.Combine(runtimeDir, "shared");
                Directory.CreateDirectory(destSharedDir);

                // Only copy NETCore.App and AspNetCore.App - skip WindowsDesktop.App
                var frameworksToCopy = new[] { "Microsoft.NETCore.App", "Microsoft.AspNetCore.App" };
                foreach (var framework in frameworksToCopy)
                {
                    var srcFrameworkDir = Path.Combine(sharedDir, framework);
                    if (Directory.Exists(srcFrameworkDir))
                    {
                        CopyDirectory(srcFrameworkDir, Path.Combine(destSharedDir, framework));
                        Log($"      Copied {framework}");
                    }
                }
            }

            // Copy host directory
            var hostDir = Path.Combine(extractDir, "host");
            if (Directory.Exists(hostDir))
            {
                CopyDirectory(hostDir, Path.Combine(runtimeDir, "host"));
            }

            // Copy dotnet executable
            var dotnetExe = isWindows ? "dotnet.exe" : "dotnet";
            var dotnetPath = Path.Combine(extractDir, dotnetExe);
            if (File.Exists(dotnetPath))
            {
                var destDotnet = Path.Combine(runtimeDir, dotnetExe);
                File.Copy(dotnetPath, destDotnet, overwrite: true);
                if (!isWindows)
                {
                    await SetExecutableAsync(destDotnet).ConfigureAwait(false);
                }
            }

            // Copy LICENSE and ThirdPartyNotices
            foreach (var file in new[] { "LICENSE.txt", "ThirdPartyNotices.txt" })
            {
                var srcFile = Path.Combine(extractDir, file);
                if (File.Exists(srcFile))
                {
                    File.Copy(srcFile, Path.Combine(runtimeDir, file), overwrite: true);
                }
            }

            // Extract dev-certs tool from SDK
            Log($"    Extracting dev-certs tool...");
            await ExtractDevCertsToolAsync(extractDir).ConfigureAwait(false);

            Log($"    SDK extraction complete");
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private Task ExtractDevCertsToolAsync(string sdkExtractDir)
    {
        // Find the dev-certs tool in sdk/<version>/DotnetTools/dotnet-dev-certs/
        var sdkDir = Path.Combine(sdkExtractDir, "sdk");
        if (!Directory.Exists(sdkDir))
        {
            Log($"    WARNING: SDK directory not found, skipping dev-certs extraction");
            return Task.CompletedTask;
        }

        // Find the SDK version directory (e.g., "10.0.102")
        var sdkVersionDirs = Directory.GetDirectories(sdkDir);
        if (sdkVersionDirs.Length == 0)
        {
            Log($"    WARNING: No SDK version directory found, skipping dev-certs extraction");
            return Task.CompletedTask;
        }

        // Use the first (should be only) SDK version directory
        var sdkVersionDir = sdkVersionDirs[0];
        var dotnetToolsDir = Path.Combine(sdkVersionDir, "DotnetTools", "dotnet-dev-certs");

        if (!Directory.Exists(dotnetToolsDir))
        {
            Log($"    WARNING: dotnet-dev-certs not found at {dotnetToolsDir}, skipping");
            return Task.CompletedTask;
        }

        // Find the tool version directory (e.g., "10.0.2-servicing.25612.105")
        var toolVersionDirs = Directory.GetDirectories(dotnetToolsDir);
        if (toolVersionDirs.Length == 0)
        {
            Log($"    WARNING: No dev-certs version directory found, skipping");
            return Task.CompletedTask;
        }

        // Find the tools/net10.0/any directory containing the actual DLLs
        var toolVersionDir = toolVersionDirs[0];
        var toolsDir = Path.Combine(toolVersionDir, "tools");

        // Look for net10.0/any or similar pattern
        string? devCertsSourceDir = null;
        if (Directory.Exists(toolsDir))
        {
            foreach (var tfmDir in Directory.GetDirectories(toolsDir))
            {
                var anyDir = Path.Combine(tfmDir, "any");
                if (Directory.Exists(anyDir) && File.Exists(Path.Combine(anyDir, "dotnet-dev-certs.dll")))
                {
                    devCertsSourceDir = anyDir;
                    break;
                }
            }
        }

        if (devCertsSourceDir is null)
        {
            Log($"    WARNING: dev-certs DLLs not found, skipping");
            return Task.CompletedTask;
        }

        // Copy to tools/dev-certs/ in the layout
        var devCertsDestDir = Path.Combine(_outputPath, "tools", "dev-certs");
        Directory.CreateDirectory(devCertsDestDir);

        // Copy the essential files
        foreach (var file in new[] { "dotnet-dev-certs.dll", "dotnet-dev-certs.deps.json", "dotnet-dev-certs.runtimeconfig.json" })
        {
            var srcFile = Path.Combine(devCertsSourceDir, file);
            if (File.Exists(srcFile))
            {
                File.Copy(srcFile, Path.Combine(devCertsDestDir, file), overwrite: true);
            }
        }

        Log($"    dev-certs tool extracted to tools/dev-certs/");
        return Task.CompletedTask;
    }

    private Task CopyNuGetHelperAsync()
    {
        Log("Copying NuGet Helper...");

        var helperPublishPath = FindPublishPath("Aspire.Cli.NuGetHelper");
        if (helperPublishPath is null)
        {
            throw new InvalidOperationException("NuGet Helper publish output not found.");
        }

        var helperDir = Path.Combine(_outputPath, "tools", "aspire-nuget");
        Directory.CreateDirectory(helperDir);

        CopyDirectory(helperPublishPath, helperDir);
        Log($"  Copied NuGet Helper to tools/aspire-nuget");

        return Task.CompletedTask;
    }

    private Task CopyAppHostServerAsync()
    {
        Log("Copying AppHost Server...");

        var serverPublishPath = FindPublishPath("Aspire.Hosting.RemoteHost");
        if (serverPublishPath is null)
        {
            throw new InvalidOperationException("AppHost Server (Aspire.Hosting.RemoteHost) publish output not found.");
        }

        var serverDir = Path.Combine(_outputPath, "aspire-server");
        Directory.CreateDirectory(serverDir);

        CopyDirectory(serverPublishPath, serverDir);
        Log($"  Copied AppHost Server to aspire-server");

        return Task.CompletedTask;
    }

    private Task CopyDashboardAsync()
    {
        Log("Copying Dashboard...");

        var dashboardPublishPath = FindPublishPath("Aspire.Dashboard");
        if (dashboardPublishPath is null)
        {
            Log("  WARNING: Dashboard publish output not found. Skipping.");
            return Task.CompletedTask;
        }

        var dashboardDir = Path.Combine(_outputPath, "dashboard");
        Directory.CreateDirectory(dashboardDir);

        CopyDirectory(dashboardPublishPath, dashboardDir);
        Log($"  Copied Dashboard to dashboard");

        return Task.CompletedTask;
    }

    private Task CopyDcpAsync()
    {
        Log("Copying DCP...");

        // DCP comes from NuGet packages, look for it in the artifacts
        var dcpPath = FindDcpPath();
        if (dcpPath is null)
        {
            Log("  WARNING: DCP not found. Skipping.");
            return Task.CompletedTask;
        }

        var dcpDir = Path.Combine(_outputPath, "dcp");
        Directory.CreateDirectory(dcpDir);

        CopyDirectory(dcpPath, dcpDir);
        Log($"  Copied DCP to dcp");

        return Task.CompletedTask;
    }

    public async Task<string> CreateArchiveAsync()
    {
        var archiveName = $"aspire-{_version}-{_rid}";
        var archivePath = Path.Combine(Path.GetDirectoryName(_outputPath)!, archiveName + ".tar.gz");

        Log($"Creating archive: {archivePath}");

        if (OperatingSystem.IsWindows())
        {
            // Use .NET TarWriter + GZip on Windows (no system tar available)
            await using var fileStream = File.Create(archivePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            await using var tarWriter = new System.Formats.Tar.TarWriter(gzipStream, leaveOpen: true);

            var topLevelDir = Path.GetFileName(_outputPath);
            foreach (var filePath in Directory.EnumerateFiles(_outputPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(_outputPath)!, filePath).Replace('\\', '/');
                await using var dataStream = File.OpenRead(filePath);
                var entry = new System.Formats.Tar.PaxTarEntry(System.Formats.Tar.TarEntryType.RegularFile, relativePath)
                {
                    DataStream = dataStream
                };
                await tarWriter.WriteEntryAsync(entry).ConfigureAwait(false);
            }
        }
        else
        {
            // Use tar for tar.gz
            var psi = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-czf \"{archivePath}\" -C \"{Path.GetDirectoryName(_outputPath)}\" \"{Path.GetFileName(_outputPath)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            // Prevent macOS from including resource forks/extended attributes in the archive
            psi.Environment["COPYFILE_DISABLE"] = "1";

            using var process = Process.Start(psi);
            if (process is not null)
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    throw new InvalidOperationException($"Failed to create archive (exit code {process.ExitCode}): {stderr}");
                }
            }
        }

        Log($"Archive created: {archivePath}");
        return archivePath;
    }

    private string? FindPublishPath(string projectName)
    {
        // Look for publish output in standard locations
        // Order matters - RID-specific single-file publish paths should come first
        var searchPaths = new[]
        {
            // Native AOT output (aspire CLI uses this)
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net10.0", _rid, "native"),
            // RID-specific single-file publish output (preferred)
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net10.0", _rid, "publish"),
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net8.0", _rid, "publish"),
            // Standard publish output
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net10.0", "publish"),
            // Arcade SDK output
            Path.Combine(_artifactsPath, "bin", projectName, "Release", _rid),
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net10.0"),
            // net8.0 for Dashboard (it targets net8.0)
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net8.0", "publish"),
            Path.Combine(_artifactsPath, "bin", projectName, "Release", "net8.0"),
            // Debug fallback
            Path.Combine(_artifactsPath, "bin", projectName, "Debug", "net10.0", _rid, "native"),
            Path.Combine(_artifactsPath, "bin", projectName, "Debug", "net10.0", _rid, "publish"),
            Path.Combine(_artifactsPath, "bin", projectName, "Debug", "net10.0", "publish"),
        };

        foreach (var path in searchPaths)
        {
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static string? FindSharedRuntime()
    {
        // Look for .NET runtime in common locations
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot))
        {
            var sharedPath = Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App");
            if (Directory.Exists(sharedPath))
            {
                // Find the latest version
                var versions = Directory.GetDirectories(sharedPath)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();
                if (versions is not null)
                {
                    return versions;
                }
            }
        }

        return null;
    }

    private string? FindDcpPath()
    {
        // DCP is in NuGet packages as Microsoft.DeveloperControlPlane.{os}-{arch}
        var nugetPackages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages");

        // Map RID to DCP package name format
        // win-x64 -> windows-amd64, linux-x64 -> linux-amd64, osx-arm64 -> darwin-arm64
        var dcpRid = _rid.ToLowerInvariant() switch
        {
            "win-x64" => "windows-amd64",
            "win-arm64" => "windows-arm64",
            "linux-x64" => "linux-amd64",
            "linux-arm64" => "linux-arm64",
            "linux-musl-x64" => "linux-musl-amd64",
            "osx-x64" => "darwin-amd64",
            "osx-arm64" => "darwin-arm64",
            _ => _rid.ToLowerInvariant()
        };

        var dcpPackageName = $"microsoft.developercontrolplane.{dcpRid}";
        var dcpPackagePath = Path.Combine(nugetPackages, dcpPackageName);

        if (Directory.Exists(dcpPackagePath))
        {
            // Find latest version (semantic versioning aware)
            var latestVersion = Directory.GetDirectories(dcpPackagePath)
                .Select(d => (Path: d, Version: Version.TryParse(Path.GetFileName(d), out var v) ? v : null))
                .Where(x => x.Version is not null)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (latestVersion.Path is not null)
            {
                var toolsPath = Path.Combine(latestVersion.Path, "tools");
                if (Directory.Exists(toolsPath))
                {
                    Log($"  Found DCP at {toolsPath}");
                    return toolsPath;
                }
            }
        }

        // Fallback: try the old Aspire.Hosting.Orchestration.{rid} package name
        var oldPackageName = $"aspire.hosting.orchestration.{_rid.ToLowerInvariant()}";
        var oldPackagePath = Path.Combine(nugetPackages, oldPackageName);

        if (Directory.Exists(oldPackagePath))
        {
            var latestVersion = Directory.GetDirectories(oldPackagePath)
                .OrderByDescending(d => d)
                .FirstOrDefault();

            if (latestVersion is not null)
            {
                var toolsPath = Path.Combine(latestVersion, "tools");
                if (Directory.Exists(toolsPath))
                {
                    Log($"  Found DCP at {toolsPath} (legacy package)");
                    return toolsPath;
                }
            }
        }

        return null;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    private static async Task SetExecutableAsync(string path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x \"{path}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        if (process is not null)
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
    }

    private void EnableRollForwardForAllTools()
    {
        Log("Enabling RollForward=Major for all tools...");

        // Find all runtimeconfig.json files in the bundle
        var runtimeConfigFiles = Directory.GetFiles(_outputPath, "*.runtimeconfig.json", SearchOption.AllDirectories);

        foreach (var configFile in runtimeConfigFiles)
        {
            try
            {
                var json = File.ReadAllText(configFile);
                using var doc = JsonDocument.Parse(json);

                // Check if rollForward is already set
                if (doc.RootElement.TryGetProperty("runtimeOptions", out var runtimeOptions) &&
                    !runtimeOptions.TryGetProperty("rollForward", out _))
                {
                    // Add rollForward: Major to the runtimeOptions
                    var updatedJson = json.Replace(
                        "\"runtimeOptions\": {",
                        "\"runtimeOptions\": {\n    \"rollForward\": \"Major\",");
                    File.WriteAllText(configFile, updatedJson);
                    Log($"  Updated: {Path.GetRelativePath(_outputPath, configFile)}");
                }
            }
            catch (Exception ex)
            {
                Log($"  WARNING: Failed to update {configFile}: {ex.Message}");
            }
        }
    }

    private void Log(string message)
    {
        if (_verbose || !message.StartsWith("  "))
        {
            Console.WriteLine(message);
        }
    }
}

#region JSON Models

[JsonSerializable(typeof(JsonElement))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
internal sealed partial class LayoutJsonContext : JsonSerializerContext
{
}

#endregion
