// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:sdk Aspire.AppHost.Sdk@13.1.0-preview.1.25603.2

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
#pragma warning disable IDE0011 // Add braces
#pragma warning disable IDE0005 // Using directive is unnecessary

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Hosting;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);
var pipeline = builder.Pipeline;

// ============================================
// CONFIGURATION
// ============================================

var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";
var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
           Environment.GetEnvironmentVariable("TF_BUILD") == "true" ||
           Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
var repoRoot = FindRepoRoot();
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

// All supported RIDs for native builds
var allRids = new[]
{
    "linux-arm64", "linux-musl-x64", "linux-x64",
    "osx-arm64", "osx-x64",
    "win-arm64", "win-x64"
};

// For local builds, only build for current platform
var targetRids = isCI ? allRids : GetCurrentPlatformRids();

// ============================================
// INIT (logs configuration)
// ============================================

pipeline.AddStep("init", async context =>
{
    context.Logger.LogInformation("Initializing build environment...");
    context.Logger.LogInformation("Configuration: {Configuration}", configuration);
    context.Logger.LogInformation("CI Mode: {IsCI}", isCI);
    context.Logger.LogInformation("Repository Root: {RepoRoot}", repoRoot);
    context.Logger.LogInformation("Target RIDs: {Rids}", string.Join(", ", targetRids));
    await Task.CompletedTask;
}, requiredBy: "restore");

// ============================================
// RESTORE
// ============================================

pipeline.AddStep("restore", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Restoring NuGet packages", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-restore", "-c", configuration],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Restore failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Restore failed: {result.Error}");
        }

        await task.CompleteAsync("Packages restored",
            CompletionState.Completed, context.CancellationToken);
    }
}, requiredBy: "build-managed");

// ============================================
// BUILD MANAGED
// ============================================

pipeline.AddStep("build-managed", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Building managed projects", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-build", "-c", configuration, "/p:SkipNativeBuild=true"],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Build failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Build failed: {result.Error}");
        }

        await task.CompleteAsync("Build completed",
            CompletionState.Completed, context.CancellationToken);
    }
}, requiredBy: "test");

// ============================================
// BUILD EXTENSION (VS Code)
// ============================================

pipeline.AddStep("build-extension", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Building VS Code extension", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-c", configuration, "--build-extension"],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Extension build failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Extension build failed: {result.Error}");
        }

        await task.CompleteAsync("Extension build completed",
            CompletionState.Completed, context.CancellationToken);
    }
}, dependsOn: "restore");

// ============================================
// NATIVE PREREQ
// ============================================

pipeline.AddStep("native-prereq", async context =>
{
    context.Logger.LogInformation("Preparing native builds for RIDs: {Rids}",
        string.Join(", ", targetRids));
    await Task.CompletedTask;
}, dependsOn: "restore");

// ============================================
// NATIVE BUILDS (Parallel by RID)
// ============================================

foreach (var rid in targetRids)
{
    pipeline.AddStep($"native-build-{rid}", async context =>
    {
        var task = await context.ReportingStep.CreateTaskAsync(
            $"Building native CLI for {rid}", context.CancellationToken);

        await using (task.ConfigureAwait(false))
        {
            // Check if we can build AOT for this RID on the current platform
            if (!CanBuildNativeForRid(rid))
            {
                context.Logger.LogWarning(
                    "Cannot build native AOT for {Rid} on current platform, skipping", rid);
                await task.CompleteAsync($"Skipped (cross-compilation not available)",
                    CompletionState.CompletedWithWarning, context.CancellationToken);
                return;
            }

            var result = await RunBuildScriptAsync(
                repoRoot, isWindows,
                ["-build", "-c", configuration, "/p:SkipManagedBuild=true", $"/p:TargetRids={rid}"],
                context.Logger,
                context.CancellationToken);

            if (!result.Success)
            {
                await task.CompleteAsync($"Native build for {rid} failed: {result.Error}",
                    CompletionState.CompletedWithError, context.CancellationToken);
                throw new InvalidOperationException($"Native build for {rid} failed: {result.Error}");
            }

            await task.CompleteAsync($"Native build for {rid} completed",
                CompletionState.Completed, context.CancellationToken);
        }
    }, dependsOn: "native-prereq", requiredBy: "native-pack");
}

// ============================================
// NATIVE PACK
// ============================================

pipeline.AddStep("native-pack", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Validating native build archives", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var packagesDir = Path.Combine(repoRoot, "artifacts", "packages", configuration);

        if (Directory.Exists(packagesDir))
        {
            var archives = Directory.GetFiles(packagesDir, "aspire-cli-*.*")
                .Where(f => f.EndsWith(".zip") || f.EndsWith(".tar.gz"))
                .ToList();

            context.Logger.LogInformation("Found {Count} native archives in {Dir}",
                archives.Count, packagesDir);

            foreach (var archive in archives)
            {
                context.Logger.LogInformation("  - {Archive}", Path.GetFileName(archive));
            }
        }
        else
        {
            context.Logger.LogWarning("Packages directory not found: {Dir}", packagesDir);
        }

        await task.CompleteAsync("Native builds validated",
            CompletionState.Completed, context.CancellationToken);
    }
}, requiredBy: "pack");

// ============================================
// TEST
// ============================================

pipeline.AddStep("test", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Running unit tests", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-test", "-c", configuration, "/p:SkipNativeBuild=true"],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Tests failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Tests failed: {result.Error}");
        }

        await task.CompleteAsync("Tests passed",
            CompletionState.Completed, context.CancellationToken);
    }
});

// ============================================
// INTEGRATION TEST
// ============================================

var runIntegrationTests = isCI ||
    Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") == "true";

if (runIntegrationTests)
{
    pipeline.AddStep("integration-test", async context =>
    {
        var task = await context.ReportingStep.CreateTaskAsync(
            "Running integration tests", context.CancellationToken);

        await using (task.ConfigureAwait(false))
        {
            var result = await RunBuildScriptAsync(
                repoRoot, isWindows,
                ["-integrationTest", "-c", configuration],
                context.Logger,
                context.CancellationToken);

            if (!result.Success)
            {
                await task.CompleteAsync($"Integration tests failed: {result.Error}",
                    CompletionState.CompletedWithError, context.CancellationToken);
                throw new InvalidOperationException($"Integration tests failed: {result.Error}");
            }

            await task.CompleteAsync("Integration tests passed",
                CompletionState.Completed, context.CancellationToken);
        }
    }, dependsOn: "test");
}

// ============================================
// PACK
// ============================================

pipeline.AddStep("pack", async context =>
{
    var task = await context.ReportingStep.CreateTaskAsync(
        "Creating NuGet packages", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-pack", "-c", configuration],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Pack failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Pack failed: {result.Error}");
        }

        await task.CompleteAsync("Packages created",
            CompletionState.Completed, context.CancellationToken);
    }
}, dependsOn: "build-managed");

// ============================================
// SIGN (CI Only)
// ============================================

if (isCI)
{
    pipeline.AddStep("sign", async context =>
    {
        var task = await context.ReportingStep.CreateTaskAsync(
            "Signing build outputs", context.CancellationToken);

        await using (task.ConfigureAwait(false))
        {
            var result = await RunBuildScriptAsync(
                repoRoot, isWindows,
                ["-sign", "-c", configuration],
                context.Logger,
                context.CancellationToken);

            if (!result.Success)
            {
                await task.CompleteAsync($"Signing failed: {result.Error}",
                    CompletionState.CompletedWithError, context.CancellationToken);
                throw new InvalidOperationException($"Signing failed: {result.Error}");
            }

            await task.CompleteAsync("Outputs signed",
                CompletionState.Completed, context.CancellationToken);
        }
    }, dependsOn: "pack", requiredBy: "artifacts");
}

// ============================================
// ARTIFACTS (Publish)
// ============================================

pipeline.AddStep("artifacts", async context =>
{
    if (!isCI)
    {
        context.Logger.LogInformation("Skipping artifact publish in local mode");
        return;
    }

    var task = await context.ReportingStep.CreateTaskAsync(
        "Publishing artifacts", context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        var result = await RunBuildScriptAsync(
            repoRoot, isWindows,
            ["-publish", "-c", configuration],
            context.Logger,
            context.CancellationToken);

        if (!result.Success)
        {
            await task.CompleteAsync($"Publish failed: {result.Error}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Publish failed: {result.Error}");
        }

        await task.CompleteAsync("Artifacts published",
            CompletionState.Completed, context.CancellationToken);
    }
}, dependsOn: isCI ? "sign" : "pack");

// ============================================
// RUN PIPELINE
// ============================================

builder.Build().Run();

// ============================================
// HELPER FUNCTIONS
// ============================================

static string FindRepoRoot()
{
    var psi = new ProcessStartInfo
    {
        FileName = "git",
        Arguments = "rev-parse --show-toplevel",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process == null)
    {
        throw new InvalidOperationException("Failed to start git process");
    }

    var output = process.StandardOutput.ReadToEnd().Trim();
    process.WaitForExit();

    if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
    {
        throw new InvalidOperationException("Failed to find git repository root");
    }

    // Convert forward slashes to platform-specific path separators
    return output.Replace('/', Path.DirectorySeparatorChar);
}

static string[] GetCurrentPlatformRids()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => ["win-x64"],
            Architecture.Arm64 => ["win-arm64"],
            _ => ["win-x64"]
        };
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        // Check if running on musl (Alpine)
        var isMusl = File.Exists("/etc/alpine-release");
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => isMusl ? ["linux-musl-x64"] : ["linux-x64"],
            Architecture.Arm64 => ["linux-arm64"],
            _ => ["linux-x64"]
        };
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => ["osx-x64"],
            Architecture.Arm64 => ["osx-arm64"],
            _ => ["osx-arm64"]
        };
    }

    return ["linux-x64"];
}

static bool CanBuildNativeForRid(string rid)
{
    // Native AOT requires building on the target platform
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        return rid.StartsWith("win-");
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        return rid.StartsWith("linux-");
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        return rid.StartsWith("osx-");
    }

    return false;
}

static async Task<(bool Success, string? Error)> RunBuildScriptAsync(
    string repoRoot,
    bool isWindows,
    string[] args,
    ILogger logger,
    CancellationToken cancellationToken)
{
    var buildScript = isWindows
        ? Path.Combine(repoRoot, "eng", "build.ps1")
        : Path.Combine(repoRoot, "eng", "build.sh");

    var executable = isWindows ? "powershell" : "bash";
    var scriptArgs = string.Join(" ", args);
    var arguments = isWindows
        ? $"-NoProfile -ExecutionPolicy Bypass -File \"{buildScript}\" {scriptArgs}"
        : $"\"{buildScript}\" {scriptArgs}";

    logger.LogDebug("Executing: {Executable} {Arguments}", executable, arguments);

    var psi = new ProcessStartInfo
    {
        FileName = executable,
        Arguments = arguments,
        WorkingDirectory = repoRoot,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(psi);
    if (process == null)
    {
        return (false, "Failed to start build process");
    }

    // Read output asynchronously to avoid deadlocks
    var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
    var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

    await process.WaitForExitAsync(cancellationToken);

    var output = await outputTask;
    var error = await errorTask;

    if (!string.IsNullOrEmpty(output))
    {
        // Log output at debug level to avoid flooding
        foreach (var line in output.Split('\n').Take(50))
        {
            logger.LogDebug("{Output}", line);
        }
        if (output.Split('\n').Length > 50)
        {
            logger.LogDebug("... (output truncated)");
        }
    }

    if (process.ExitCode != 0)
    {
        logger.LogError("Build failed with exit code {ExitCode}", process.ExitCode);
        if (!string.IsNullOrEmpty(error))
        {
            logger.LogError("{Error}", error);
        }
        return (false, string.IsNullOrEmpty(error) ? $"Exit code {process.ExitCode}" : error);
    }

    return (true, null);
}
