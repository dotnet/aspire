// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

public sealed class EnvironmentDiagnosticsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DumpNuGetSignatureDiagnostics()
    {
        // 1. Dump all environment variables
        Log("========================================");
        Log("=== 1. Environment Variables ===");
        Log("========================================");

        var envVars = Environment.GetEnvironmentVariables();
        var sorted = envVars.Keys.Cast<string>().Order();

        foreach (var key in sorted)
        {
            Log($"  {key} = {envVars[key]}");
        }

        // 2. .NET SDK version
        Log("");
        Log("========================================");
        Log("=== 2. .NET SDK Version ===");
        Log("========================================");

        await RunCommandAsync("dotnet", "--version");
        await RunCommandAsync("dotnet", "--info");

        // 3. DOTNET_NUGET_SIGNATURE_VERIFICATION env var
        Log("");
        Log("========================================");
        Log("=== 3. DOTNET_NUGET_SIGNATURE_VERIFICATION ===");
        Log("========================================");

        var sigVerification = Environment.GetEnvironmentVariable("DOTNET_NUGET_SIGNATURE_VERIFICATION");
        Log($"  DOTNET_NUGET_SIGNATURE_VERIFICATION = {(sigVerification ?? "(not set)")}");

        // 4. NuGet configs (all levels) and their content
        Log("");
        Log("========================================");
        Log("=== 4. NuGet.config Files (all levels) ===");
        Log("========================================");

        var configPaths = GetNuGetConfigPaths();

        foreach (var path in configPaths)
        {
            Log($"--- Checking: {path} ---");

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                Log($"  [FOUND] {path}");
                Log(content);
            }
            else
            {
                Log($"  [NOT FOUND] {path}");
            }

            Log("");
        }

        // 5. Effective merged NuGet config
        Log("========================================");
        Log("=== 5. Effective Merged NuGet Config (dotnet nuget config) ===");
        Log("========================================");

        await RunCommandAsync("dotnet", "nuget config --show-all");

        // 6. NuGet package sources
        Log("");
        Log("========================================");
        Log("=== 6. NuGet Package Sources ===");
        Log("========================================");

        await RunCommandAsync("dotnet", "nuget list source");

        // 7. NuGet package cache
        Log("");
        Log("========================================");
        Log("=== 7. NuGet Package Cache ===");
        Log("========================================");

        var nugetCachePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"),
            Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? "(NUGET_PACKAGES not set)",
        };

        foreach (var cachePath in nugetCachePaths.Distinct())
        {
            Log($"  Cache path: {cachePath}");
            if (Directory.Exists(cachePath))
            {
                Log($"    [EXISTS] - listing microsoft.extensions.primitives versions:");
                var primitivesDir = Path.Combine(cachePath, "microsoft.extensions.primitives");
                if (Directory.Exists(primitivesDir))
                {
                    foreach (var versionDir in Directory.GetDirectories(primitivesDir))
                    {
                        Log($"      {Path.GetFileName(versionDir)}");
                    }
                }
                else
                {
                    Log("      (package not cached)");
                }
            }
            else
            {
                Log($"    [DOES NOT EXIST]");
            }
        }

        // 8. System certificate store (Linux-specific)
        Log("");
        Log("========================================");
        Log("=== 8. System Certificate Store ===");
        Log("========================================");

        if (!OperatingSystem.IsWindows())
        {
            // Code-signing bundle
            var objSignBundle = "/etc/pki/ca-trust/extracted/pem/objsign-ca-bundle.pem";
            Log($"  Code-signing bundle: {objSignBundle}");
            if (File.Exists(objSignBundle))
            {
                await RunCommandAsync("ls", $"-la {objSignBundle}");
            }
            else
            {
                Log("    [NOT FOUND]");
            }

            // General CA certs
            Log("  General CA certs directory: /etc/ssl/certs/");
            if (Directory.Exists("/etc/ssl/certs"))
            {
                await RunCommandAsync("ls", "/etc/ssl/certs/ | head -30");
                var certCount = Directory.GetFiles("/etc/ssl/certs").Length;
                Log($"    Total files in /etc/ssl/certs: {certCount}");
            }
            else
            {
                Log("    [DIRECTORY NOT FOUND]");
            }

            // Also check alternatives
            var altCertPaths = new[] { "/etc/pki/tls/certs", "/usr/share/ca-certificates" };
            foreach (var certPath in altCertPaths)
            {
                Log($"  Alternate cert path: {certPath}");
                if (Directory.Exists(certPath))
                {
                    var count = Directory.GetFiles(certPath, "*", SearchOption.AllDirectories).Length;
                    Log($"    [EXISTS] - {count} files");
                }
                else
                {
                    Log($"    [NOT FOUND]");
                }
            }
        }
        else
        {
            Log("  (Windows - certificates managed via Windows certificate store, skipping Linux-specific checks)");
        }

        // 9. Verify a specific package signature
        Log("");
        Log("========================================");
        Log("=== 9. NuGet Package Signature Verification ===");
        Log("========================================");

        var packageCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        var primitivesPath = Path.Combine(packageCache, "microsoft.extensions.primitives");
        if (Directory.Exists(primitivesPath))
        {
            var versionDirs = Directory.GetDirectories(primitivesPath);
            if (versionDirs.Length > 0)
            {
                // Pick the first version found
                var versionDir = versionDirs[0];
                var nupkgs = Directory.GetFiles(versionDir, "*.nupkg");
                if (nupkgs.Length > 0)
                {
                    Log($"  Verifying: {nupkgs[0]}");
                    await RunCommandAsync("dotnet", $"nuget verify --all \"{nupkgs[0]}\"");
                }
                else
                {
                    Log($"  No .nupkg found in {versionDir}");
                }
            }
            else
            {
                Log("  No versions found for microsoft.extensions.primitives");
            }
        }
        else
        {
            Log($"  microsoft.extensions.primitives not found in cache at {primitivesPath}");
        }

        Log("");
        Log("========================================");
        Log("=== Diagnostics Complete ===");
        Log("========================================");
    }

    private void Log(string message)
    {
        output.WriteLine(message);
        Console.WriteLine(message);
    }

    private async Task RunCommandAsync(string command, string arguments)
    {
        Log($"  > {command} {arguments}");

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            var stdOut = await process.StandardOutput.ReadToEndAsync();
            var stdErr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdOut))
            {
                Log(stdOut.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(stdErr))
            {
                Log($"  [stderr] {stdErr.TrimEnd()}");
            }

            Log($"  [exit code: {process.ExitCode}]");
        }
        catch (Exception ex)
        {
            Log($"  [ERROR running '{command} {arguments}': {ex.Message}]");
        }
    }

    private static List<string> GetNuGetConfigPaths()
    {
        var paths = new List<string>();

        // Current directory and ancestors (project-level walk)
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            paths.Add(Path.Combine(dir, "NuGet.config"));
            paths.Add(Path.Combine(dir, "nuget.config"));
            dir = Path.GetDirectoryName(dir);
        }

        // User-level NuGet config
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!string.IsNullOrEmpty(appData))
            {
                paths.Add(Path.Combine(appData, "NuGet", "NuGet.Config"));
            }
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                paths.Add(Path.Combine(home, ".nuget", "NuGet", "NuGet.Config"));
            }
        }

        // Machine-level NuGet configs
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetEnvironmentVariable("ProgramData");
            if (!string.IsNullOrEmpty(programData))
            {
                var machineDir = Path.Combine(programData, "NuGet", "Config");
                if (Directory.Exists(machineDir))
                {
                    foreach (var file in Directory.GetFiles(machineDir, "*.config", SearchOption.AllDirectories))
                    {
                        paths.Add(file);
                    }
                }
                else
                {
                    paths.Add(machineDir + " (directory does not exist)");
                }
            }
        }
        else
        {
            // Linux machine-wide locations
            var machineDirs = new[]
            {
                "/etc/dotnet/NuGet/NuGet.Config",
                "/etc/opt/NuGet/Config",
                "/etc/NuGet/Config",
            };

            foreach (var machinePath in machineDirs)
            {
                if (File.Exists(machinePath))
                {
                    paths.Add(machinePath);
                }
                else if (Directory.Exists(machinePath))
                {
                    foreach (var file in Directory.GetFiles(machinePath, "*.config", SearchOption.AllDirectories))
                    {
                        paths.Add(file);
                    }
                }
                else
                {
                    paths.Add(machinePath + " (not found)");
                }
            }
        }

        // Deduplicate preserving order
        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
