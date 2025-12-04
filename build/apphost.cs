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
// CONFIGURATION (from environment variables)
// ============================================

var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") ?? "Debug";
var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
           Environment.GetEnvironmentVariable("TF_BUILD") == "true" ||
           Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
var repoRoot = FindRepoRoot();
var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

// CI-specific configuration
var signType = Environment.GetEnvironmentVariable("SIGN_TYPE") ?? (isCI ? "real" : "test");
var shouldSign = Environment.GetEnvironmentVariable("SIGN") == "true" || isCI;
var shouldPublish = Environment.GetEnvironmentVariable("PUBLISH") == "true" || isCI;
var officialBuildId = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER") ?? "";
var teamName = Environment.GetEnvironmentVariable("TEAM_NAME") ?? "aspire";
var versionSuffix = Environment.GetEnvironmentVariable("VERSION_SUFFIX") ?? "";
var buildExtension = Environment.GetEnvironmentVariable("BUILD_EXTENSION") == "true";
var skipTests = Environment.GetEnvironmentVariable("SKIP_TESTS") == "true";
var skipManagedBuild = Environment.GetEnvironmentVariable("SKIP_MANAGED_BUILD") == "true";
var continuousIntegrationBuild = isCI || Environment.GetEnvironmentVariable("ContinuousIntegrationBuild") == "true";

// Binlog path
var binlogPath = Environment.GetEnvironmentVariable("BINLOG_PATH") ??
    Path.Combine(repoRoot, "artifacts", "log", configuration);

// All supported RIDs for native builds
var allRids = new[]
{
    "linux-arm64", "linux-musl-x64", "linux-x64",
    "osx-arm64", "osx-x64",
    "win-arm64", "win-x64"
};

// TARGET_RIDS env var can override (colon-separated like "win-x64:linux-x64")
var targetRidsEnv = Environment.GetEnvironmentVariable("TARGET_RIDS");
var targetRids = !string.IsNullOrEmpty(targetRidsEnv)
    ? targetRidsEnv.Split(':', StringSplitOptions.RemoveEmptyEntries)
    : (isCI ? allRids : GetCurrentPlatformRids());

// Build common MSBuild args
var commonMSBuildArgs = BuildCommonMSBuildArgs();

// ============================================
// INIT (logs configuration)
// ============================================

pipeline.AddStep("init", async context =>
{
    context.Logger.LogInformation("=== Aspire Build Pipeline ===");
    context.Logger.LogInformation("Configuration: {Configuration}", configuration);
    context.Logger.LogInformation("CI Mode: {IsCI}", isCI);
    context.Logger.LogInformation("Repository Root: {RepoRoot}", repoRoot);
    context.Logger.LogInformation("Target RIDs: {Rids}", string.Join(", ", targetRids));
    context.Logger.LogInformation("Sign: {Sign} (Type: {SignType})", shouldSign, signType);
    context.Logger.LogInformation("Publish: {Publish}", shouldPublish);
    context.Logger.LogInformation("Build Extension: {BuildExtension}", buildExtension);
    if (!string.IsNullOrEmpty(officialBuildId))
        context.Logger.LogInformation("Official Build ID: {BuildId}", officialBuildId);
    if (!string.IsNullOrEmpty(versionSuffix))
        context.Logger.LogInformation("Version Suffix: {VersionSuffix}", versionSuffix);

    Directory.CreateDirectory(binlogPath);
    await Task.CompletedTask;
}, requiredBy: "restore");

// ============================================
// RESTORE
// ============================================

pipeline.AddStep("restore", async context =>
{
    var args = $"restore \"{Path.Combine(repoRoot, "Aspire.slnx")}\" " +
               $"/bl:\"{Path.Combine(binlogPath, "restore.binlog")}\" " +
               commonMSBuildArgs;

    await RunStepAsync(context, "Restoring NuGet packages", "dotnet", args);
}, requiredBy: "build-core");

// ============================================
// BUILD CORE FRAMEWORK (Sequential - blocks everything)
// ============================================

if (!skipManagedBuild)
{
    pipeline.AddStep("build-core", async context =>
    {
        var args = $"build \"{Path.Combine(repoRoot, "src", "Aspire.Hosting", "Aspire.Hosting.csproj")}\" " +
                   $"--no-restore -c {configuration} " +
                   $"/bl:\"{Path.Combine(binlogPath, "build-core.binlog")}\" " +
                   commonMSBuildArgs;

        await RunStepAsync(context, "Building Aspire.Hosting (core)", "dotnet", args);
    }, requiredBy: new[] { "build-hosting-modules", "build-dashboard", "build-sdk", "build-cli" });

    // ============================================
    // BUILD PARALLEL GROUPS (Can run in parallel)
    // ============================================

    pipeline.AddStep("build-hosting-modules", async context =>
    {
        var hostingDir = Path.Combine(repoRoot, "src");
        var hostingProjects = Directory.GetFiles(hostingDir, "Aspire.Hosting.*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("Aspire.Hosting.csproj") &&
                        !p.Contains("Aspire.Hosting.Tests") &&
                        !p.Contains("Aspire.Hosting.Sdk"))
            .ToList();

        context.Logger.LogInformation("Building {Count} hosting modules", hostingProjects.Count);

        var args = $"build \"{Path.Combine(repoRoot, "Aspire.slnx")}\" " +
                   $"--no-restore -c {configuration} " +
                   $"/p:SkipTestProjects=true /p:SkipPlaygroundProjects=true " +
                   $"--no-dependencies /p:BuildProjectReferences=false " +
                   $"/bl:\"{Path.Combine(binlogPath, "build-hosting-modules.binlog")}\" " +
                   commonMSBuildArgs;

        await RunStepAsync(context, $"Building {hostingProjects.Count} hosting modules", "dotnet", args);
    }, dependsOn: "build-core", requiredBy: new[] { "build-managed", "build-tests" });

    pipeline.AddStep("build-dashboard", async context =>
    {
        var dashboardProj = Path.Combine(repoRoot, "src", "Aspire.Dashboard", "Aspire.Dashboard.csproj");
        if (File.Exists(dashboardProj))
        {
            var args = $"build \"{dashboardProj}\" --no-restore -c {configuration} " +
                       $"/bl:\"{Path.Combine(binlogPath, "build-dashboard.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, "Building Aspire.Dashboard", "dotnet", args);
        }
    }, dependsOn: "build-core", requiredBy: "build-managed");

    pipeline.AddStep("build-sdk", async context =>
    {
        var sdkProj = Path.Combine(repoRoot, "src", "Aspire.AppHost.Sdk", "Aspire.AppHost.Sdk.csproj");
        if (File.Exists(sdkProj))
        {
            var args = $"build \"{sdkProj}\" --no-restore -c {configuration} " +
                       $"/bl:\"{Path.Combine(binlogPath, "build-sdk.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, "Building Aspire.AppHost.Sdk", "dotnet", args);
        }
    }, dependsOn: "build-core", requiredBy: "build-managed");

    pipeline.AddStep("build-cli", async context =>
    {
        var cliProj = Path.Combine(repoRoot, "src", "Aspire.Cli", "Aspire.Cli.csproj");
        if (File.Exists(cliProj))
        {
            var args = $"build \"{cliProj}\" --no-restore -c {configuration} " +
                       $"/bl:\"{Path.Combine(binlogPath, "build-cli.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, "Building Aspire.Cli", "dotnet", args);
        }
    }, dependsOn: "build-core", requiredBy: "build-managed");

    // ============================================
    // BUILD-MANAGED (Meta-step for all managed code)
    // ============================================

    pipeline.AddStep("build-managed", async context =>
    {
        context.Logger.LogInformation("All managed code built successfully");
        await Task.CompletedTask;
    });
}

// ============================================
// BUILD VS CODE EXTENSION (Optional)
// ============================================

if (buildExtension)
{
    var extensionDeps = skipManagedBuild ? "restore" : "build-managed";
    pipeline.AddStep("build-extension", async context =>
    {
        var extensionDir = Path.Combine(repoRoot, "extension");
        if (Directory.Exists(extensionDir))
        {
            // Install dependencies
            await RunStepAsync(context, "Installing extension dependencies",
                "yarn", "install", extensionDir);

            // Build extension
            await RunStepAsync(context, "Building VS Code extension",
                "yarn", "build", extensionDir);

            // Package VSIX
            await RunStepAsync(context, "Packaging VSIX",
                "npx", "@vscode/vsce package --yarn --pre-release -o out/aspire-extension.vsix", extensionDir);
        }
    }, dependsOn: extensionDeps, requiredBy: "pack");
}

// ============================================
// BUILD TESTS (After src is complete)
// ============================================

if (!skipTests && !skipManagedBuild)
{
    pipeline.AddStep("build-tests", async context =>
    {
        var testsDir = Path.Combine(repoRoot, "tests");
        if (Directory.Exists(testsDir))
        {
            var testProjects = Directory.GetFiles(testsDir, "*.csproj", SearchOption.AllDirectories);
            context.Logger.LogInformation("Building {Count} test projects", testProjects.Length);

            var args = $"build \"{Path.Combine(repoRoot, "Aspire.slnx")}\" " +
                       $"--no-restore -c {configuration} " +
                       $"/p:BuildTestsOnly=true /p:SkipPlaygroundProjects=true " +
                       $"/bl:\"{Path.Combine(binlogPath, "build-tests.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, $"Building {testProjects.Length} test projects", "dotnet", args);
        }
    }, requiredBy: "test");
}

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
        if (!CanBuildNativeForRid(rid))
        {
            context.Logger.LogWarning(
                "Cannot build native AOT for {Rid} on current platform, skipping", rid);
            return;
        }

        var cliPackProj = Path.Combine(repoRoot, "eng", "clipack", $"Aspire.Cli.{rid}.csproj");
        if (!File.Exists(cliPackProj))
        {
            context.Logger.LogWarning("CLI pack project not found: {Path}", cliPackProj);
            return;
        }

        var args = $"build \"{cliPackProj}\" -c {configuration} " +
                   $"/bl:\"{Path.Combine(binlogPath, $"native-build-{rid}.binlog")}\" " +
                   commonMSBuildArgs;

        await RunStepAsync(context, $"Building native CLI for {rid}", "dotnet", args);
    }, dependsOn: "native-prereq", requiredBy: "native-pack");
}

// ============================================
// NATIVE PACK
// ============================================

pipeline.AddStep("native-pack", async context =>
{
    var packagesDir = Path.Combine(repoRoot, "artifacts", "packages", configuration);

    if (Directory.Exists(packagesDir))
    {
        var archives = Directory.GetFiles(packagesDir, "aspire-cli-*.*", SearchOption.AllDirectories)
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
}, requiredBy: "pack");

// ============================================
// TEST - Run tests in parallel groups
// ============================================

if (!skipTests && !skipManagedBuild)
{
    pipeline.AddStep("test", async context =>
    {
        context.Logger.LogInformation("All tests completed");
        await Task.CompletedTask;
    });

    pipeline.AddStep("test-hosting", async context =>
    {
        var testProj = Path.Combine(repoRoot, "tests", "Aspire.Hosting.Tests", "Aspire.Hosting.Tests.csproj");
        if (File.Exists(testProj))
        {
            var args = $"test \"{testProj}\" --no-build -c {configuration} " +
                       $"--filter \"Category!=failing&Category!=OuterLoop\" " +
                       $"/bl:\"{Path.Combine(binlogPath, "test-hosting.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, "Running Aspire.Hosting.Tests", "dotnet", args);
        }
    }, dependsOn: "build-tests", requiredBy: "test");

    pipeline.AddStep("test-dashboard", async context =>
    {
        var testProj = Path.Combine(repoRoot, "tests", "Aspire.Dashboard.Tests", "Aspire.Dashboard.Tests.csproj");
        if (File.Exists(testProj))
        {
            var args = $"test \"{testProj}\" --no-build -c {configuration} " +
                       $"--filter \"Category!=failing&Category!=OuterLoop\" " +
                       $"/bl:\"{Path.Combine(binlogPath, "test-dashboard.binlog")}\" " +
                       commonMSBuildArgs;

            await RunStepAsync(context, "Running Aspire.Dashboard.Tests", "dotnet", args);
        }
    }, dependsOn: "build-tests", requiredBy: "test");

    pipeline.AddStep("test-other", async context =>
    {
        var testsDir = Path.Combine(repoRoot, "tests");
        var otherTestDirs = Directory.GetDirectories(testsDir, "Aspire.*.Tests")
            .Where(d => !d.Contains("Hosting") && !d.Contains("Dashboard") && !d.Contains("EndToEnd"))
            .Take(10)
            .ToList();

        context.Logger.LogInformation("Running {Count} other test projects", otherTestDirs.Count);

        foreach (var testDir in otherTestDirs)
        {
            var testProj = Directory.GetFiles(testDir, "*.csproj").FirstOrDefault();
            if (testProj != null)
            {
                var projName = Path.GetFileNameWithoutExtension(testProj);
                var args = $"test \"{testProj}\" --no-build -c {configuration} " +
                           $"--filter \"Category!=failing&Category!=OuterLoop\" " +
                           commonMSBuildArgs;

                await RunStepAsync(context, $"Running {projName}", "dotnet", args);
            }
        }
    }, dependsOn: "build-tests", requiredBy: "test");

    // Integration tests (optional, CI or explicit)
    var runIntegrationTests = isCI ||
        Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") == "true";

    if (runIntegrationTests)
    {
        pipeline.AddStep("integration-test", async context =>
        {
            var testProj = Path.Combine(repoRoot, "tests", "Aspire.EndToEnd.Tests", "Aspire.EndToEnd.Tests.csproj");
            if (File.Exists(testProj))
            {
                var args = $"test \"{testProj}\" --no-build -c {configuration} " +
                           $"--filter \"Category!=failing\" " +
                           $"/bl:\"{Path.Combine(binlogPath, "integration-test.binlog")}\" " +
                           commonMSBuildArgs;

                await RunStepAsync(context, "Running Aspire.EndToEnd.Tests", "dotnet", args);
            }
        }, dependsOn: "test");
    }
}

// ============================================
// PACK
// ============================================

var packDependsOn = skipManagedBuild
    ? new[] { "native-pack" }
    : new[] { "build-hosting-modules", "build-dashboard", "build-sdk", "build-cli", "native-pack" };

pipeline.AddStep("pack", async context =>
{
    var args = $"pack \"{Path.Combine(repoRoot, "Aspire.slnx")}\" " +
               $"--no-build -c {configuration} " +
               $"/p:SkipTestProjects=true /p:SkipPlaygroundProjects=true " +
               $"/bl:\"{Path.Combine(binlogPath, "pack.binlog")}\" " +
               commonMSBuildArgs;

    await RunStepAsync(context, "Creating NuGet packages", "dotnet", args);
}, dependsOn: packDependsOn);

// ============================================
// SIGN (CI Only)
// ============================================

if (shouldSign && isCI)
{
    pipeline.AddStep("sign", async context =>
    {
        var signArgs = $"/p:DotNetSignType={signType} /p:TeamName={teamName} /p:Sign=true";

        await RunStepAsync(context, "Signing build outputs",
            isWindows ? "powershell" : "bash",
            BuildScriptArgs(repoRoot, isWindows, "-sign", "-c", configuration, signArgs));
    }, dependsOn: "pack", requiredBy: "artifacts");
}

// ============================================
// ARTIFACTS (Publish)
// ============================================

pipeline.AddStep("artifacts", async context =>
{
    if (!shouldPublish)
    {
        context.Logger.LogInformation("Skipping artifact publish (not in CI or PUBLISH not set)");
        return;
    }

    var publishArgs = "/p:DotNetPublishUsingPipelines=true";

    await RunStepAsync(context, "Publishing artifacts",
        isWindows ? "powershell" : "bash",
        BuildScriptArgs(repoRoot, isWindows, "-publish", "-c", configuration, publishArgs));
}, dependsOn: shouldSign && isCI ? "sign" : "pack");

// ============================================
// RUN PIPELINE
// ============================================

builder.Build().Run();

// ============================================
// HELPER FUNCTIONS
// ============================================

string BuildCommonMSBuildArgs()
{
    var args = new List<string>();

    if (continuousIntegrationBuild)
        args.Add("/p:ContinuousIntegrationBuild=true");

    if (!string.IsNullOrEmpty(officialBuildId))
        args.Add($"/p:OfficialBuildId={officialBuildId}");

    if (!string.IsNullOrEmpty(versionSuffix))
        args.Add($"/p:VersionSuffix={versionSuffix}");

    if (targetRids.Length > 0)
        args.Add($"/p:TargetRids={string.Join(":", targetRids)}");

    return string.Join(" ", args);
}

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
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return rid.StartsWith("win-");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return rid.StartsWith("linux-");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return rid.StartsWith("osx-");

    return false;
}

static string BuildScriptArgs(string repoRoot, bool isWindows, params string[] args)
{
    var buildScript = isWindows
        ? Path.Combine(repoRoot, "eng", "build.ps1")
        : Path.Combine(repoRoot, "eng", "build.sh");

    var scriptArgs = string.Join(" ", args);
    return isWindows
        ? $"-NoProfile -ExecutionPolicy Bypass -File \"{buildScript}\" {scriptArgs}"
        : $"\"{buildScript}\" {scriptArgs}";
}

static async Task RunStepAsync(
    PipelineStepContext context,
    string description,
    string executable,
    string arguments,
    string? workingDirectory = null)
{
    workingDirectory ??= FindRepoRoot();

    var task = await context.ReportingStep.CreateTaskAsync(description, context.CancellationToken);

    await using (task.ConfigureAwait(false))
    {
        context.Logger.LogDebug("Executing: {Executable} {Arguments}", executable, arguments);

        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            await task.CompleteAsync("Failed to start process",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"Failed to start: {executable}");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(context.CancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(context.CancellationToken);

        await process.WaitForExitAsync(context.CancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrEmpty(output))
        {
            foreach (var line in output.Split('\n').Take(50))
            {
                context.Logger.LogDebug("{Output}", line);
            }
            if (output.Split('\n').Length > 50)
            {
                context.Logger.LogDebug("... (output truncated)");
            }
        }

        if (process.ExitCode != 0)
        {
            context.Logger.LogError("Command failed with exit code {ExitCode}", process.ExitCode);
            if (!string.IsNullOrEmpty(error))
            {
                context.Logger.LogError("{Error}", error);
            }
            var errorMsg = string.IsNullOrEmpty(error) ? $"Exit code {process.ExitCode}" : error;
            await task.CompleteAsync($"Failed: {errorMsg}",
                CompletionState.CompletedWithError, context.CancellationToken);
            throw new InvalidOperationException($"{description} failed: {errorMsg}");
        }

        await task.CompleteAsync("Completed", CompletionState.Completed, context.CancellationToken);
    }
}
