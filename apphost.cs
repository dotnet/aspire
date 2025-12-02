// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Aspire DevTools AppHost - Run developer scripts and tools via Aspire Dashboard
// To run this app in this repo use the following command line to ensure latest changes are always picked up:
// $ dotnet apphost.cs --no-cache

// These directives are not required in regular apps, only here in the aspire repo itself
/*
#:sdk Aspire.AppHost.Sdk
*/
#:property IsAspireHost=true
#:property PublishAot=false

var builder = DistributedApplication.CreateBuilder(args);

// Determine the shell to use based on platform
var isWindows = OperatingSystem.IsWindows();
var shell = isWindows ? "pwsh" : "bash";
var scriptExt = isWindows ? ".ps1" : ".sh";

// =============================================================================
// BUILD TOOLS
// =============================================================================

builder.AddExecutable("restore", shell, ".", isWindows ? "./restore.cmd" : "./restore.sh")
    .WithIconName("ArrowDownload")
    .WithExplicitStart();

builder.AddExecutable("build-debug", shell, ".", $"./eng/build{scriptExt}", "-restore", "-build", "-configuration", "Debug")
    .WithIconName("Wrench")
    .WithExplicitStart();

builder.AddExecutable("build-release", shell, ".", $"./eng/build{scriptExt}", "-restore", "-build", "-configuration", "Release")
    .WithIconName("Wrench")
    .WithExplicitStart();

builder.AddExecutable("build-pack", shell, ".", $"./eng/build{scriptExt}", "-restore", "-build", "-pack", "-configuration", "Release")
    .WithIconName("BoxMultiple")
    .WithExplicitStart();

builder.AddExecutable("run-tests", shell, ".", $"./eng/build{scriptExt}", "-restore", "-build", "-test", "-configuration", "Debug")
    .WithIconName("BeakerEdit")
    .WithExplicitStart();

builder.AddExecutable("build-extension", shell, "./extension", "./build.sh")
    .WithIconName("ExtensionPuzzle")
    .WithExplicitStart();

// =============================================================================
// LOCAL CLI DEVELOPMENT
// =============================================================================

// Create local hive with Debug packages
builder.AddExecutable("localhive-debug", shell, ".", $"./localhive{scriptExt}", "-c", "Debug", "-n", "dev")
    .WithIconName("FolderAdd")
    .WithExplicitStart();

// Create local hive with Release packages
builder.AddExecutable("localhive-release", shell, ".", $"./localhive{scriptExt}", "-c", "Release", "-n", "local")
    .WithIconName("FolderAdd")
    .WithExplicitStart();

// =============================================================================
// CLI INSTALLATION
// =============================================================================

// Get Aspire CLI (latest release)
builder.AddExecutable("get-cli-release", shell, "./eng/scripts", $"./get-aspire-cli{scriptExt}", "--quality", "release")
    .WithIconName("ArrowDownload")
    .WithExplicitStart();

// Get Aspire CLI (staging/preview)
builder.AddExecutable("get-cli-staging", shell, "./eng/scripts", $"./get-aspire-cli{scriptExt}", "--quality", "staging")
    .WithIconName("ArrowDownload")
    .WithExplicitStart();

// Get Aspire CLI (dev builds)
builder.AddExecutable("get-cli-dev", shell, "./eng/scripts", $"./get-aspire-cli{scriptExt}", "--quality", "dev")
    .WithIconName("ArrowDownload")
    .WithExplicitStart();

// =============================================================================
// ANALYSIS & RELEASE NOTES TOOLS
// =============================================================================

// Find missing NuGet packages for internal feeds
builder.AddExecutable("find-missing-packages", shell, "./eng", $"./find-missing-packages{scriptExt}")
    .WithIconName("Search")
    .WithExplicitStart();

// Extract API changes (builds and generates API diff)
builder.AddExecutable("extract-api-changes", shell, "./tools/ReleaseNotes", $"./extract-api-changes{scriptExt}")
    .WithIconName("DocumentData")
    .WithExplicitStart();

// Analyze all components for release notes
builder.AddExecutable("analyze-components", shell, "./tools/ReleaseNotes", $"./analyze-all-components{scriptExt}")
    .WithIconName("DocumentBulletList")
    .WithExplicitStart();

// =============================================================================
// MANIFEST REFRESH
// =============================================================================

// Refresh playground manifests (PowerShell script, works on both platforms)
builder.AddExecutable("refresh-manifests", "pwsh", ".", "./eng/refreshManifests.ps1")
    .WithIconName("ArrowSync")
    .WithExplicitStart();

// =============================================================================
// GIT UTILITIES
// =============================================================================

// Show git status
builder.AddExecutable("git-status", "git", ".", "status")
    .WithIconName("BranchFork")
    .WithExplicitStart();

// Show recent commits
builder.AddExecutable("git-log", "git", ".", "log", "--oneline", "-20")
    .WithIconName("History")
    .WithExplicitStart();

// Show current diff
builder.AddExecutable("git-diff", "git", ".", "diff")
    .WithIconName("DocumentDiff")
    .WithExplicitStart();

// =============================================================================
// QUICK UTILITIES
// =============================================================================

// Clean build artifacts
builder.AddExecutable("clean", shell, ".", $"./eng/build{scriptExt}", "-clean")
    .WithIconName("Delete")
    .WithExplicitStart();

// Format code
builder.AddExecutable("format", "dotnet", ".", "format", "--verbosity", "normal")
    .WithIconName("TextAlignLeft")
    .WithExplicitStart();

// Run dotnet with local SDK
builder.AddExecutable("dotnet-info", shell, ".", isWindows ? "./dotnet.cmd" : "./dotnet.sh", "--info")
    .WithIconName("Info")
    .WithExplicitStart();

builder.Build().Run();
