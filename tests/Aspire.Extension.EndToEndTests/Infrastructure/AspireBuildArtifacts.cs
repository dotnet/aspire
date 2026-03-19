// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Extension.EndToEndTests.Infrastructure;

/// <summary>
/// Paths to locally-built Aspire artifacts that are volume-mounted into the Docker container.
/// Mirrors the CLI E2E pattern: build on host, mount read-only into container.
/// </summary>
internal sealed record AspireBuildArtifacts(
    string CliPublishDirectory,
    string PackagesDirectory,
    string VsixPath,
    string NuGetConfigPath)
{
    /// <summary>
    /// Well-known mount paths inside the Docker container.
    /// </summary>
    internal static class ContainerPaths
    {
        public const string CliMount = "/opt/aspire-cli";
        public const string PackagesMount = "/opt/aspire/packages";
        public const string VsixMount = "/opt/aspire/extension.vsix";
        public const string NuGetConfigMount = "/opt/aspire/nuget.config";
    }

    /// <summary>
    /// Detects locally-built artifacts from a prior <c>./build.sh --bundle --build-extension --pack</c> run.
    /// Returns <c>null</c> if any required artifact is missing.
    /// </summary>
    /// <remarks>
    /// Set <c>ASPIRE_ARTIFACTS_ROOT</c> to point to a different repo's artifacts directory
    /// (e.g., when running tests from a git worktree where the build was done in the main repo).
    /// </remarks>
    public static AspireBuildArtifacts? Detect(string repoRoot)
    {
        // Allow override for worktree scenarios where artifacts live in the main repo
        var artifactsRoot = Environment.GetEnvironmentVariable("ASPIRE_ARTIFACTS_ROOT");
        var effectiveRoot = !string.IsNullOrEmpty(artifactsRoot) ? artifactsRoot : repoRoot;

        // 1. CLI binary: artifacts/bin/Aspire.Cli/*/net*/linux-x64/publish/aspire
        //    Same detection logic as CliE2ETestHelpers.FindLocalCliBinary()
        var cliPublishDir = FindCliPublishDirectory(effectiveRoot);

        // 2. NuGet packages: artifacts/packages/Debug/Shipping/ (or Release)
        var packagesDir = FindPackagesDirectory(effectiveRoot);

        // 3. VSIX: artifacts/packages/Debug/vscode/aspire-vscode-*.vsix (or Release)
        var vsixPath = FindVsixPath(effectiveRoot);

        // 4. NuGet config template (always relative to the repo, not artifacts override)
        var nugetConfigPath = Path.Combine(repoRoot, "tests", "Shared", "TemplatesTesting", "data", "nuget8.config");

        if (cliPublishDir is null || packagesDir is null || vsixPath is null || !File.Exists(nugetConfigPath))
        {
            return null;
        }

        return new AspireBuildArtifacts(cliPublishDir, packagesDir, vsixPath, nugetConfigPath);
    }

    /// <summary>
    /// Describes what's missing so the developer knows what to build.
    /// </summary>
    public static string DescribeMissing(string repoRoot)
    {
        var artifactsRoot = Environment.GetEnvironmentVariable("ASPIRE_ARTIFACTS_ROOT");
        var effectiveRoot = !string.IsNullOrEmpty(artifactsRoot) ? artifactsRoot : repoRoot;
        var missing = new List<string>();

        if (FindCliPublishDirectory(effectiveRoot) is null)
        {
            missing.Add("CLI native binary (run: ./build.sh --bundle)");
        }

        if (FindPackagesDirectory(effectiveRoot) is null)
        {
            missing.Add("NuGet packages (run: ./localhive.sh or ./build.sh --pack)");
        }

        if (FindVsixPath(effectiveRoot) is null)
        {
            missing.Add("VS Code extension VSIX (run: ./build.sh --build-extension)");
        }

        var nugetConfig = Path.Combine(repoRoot, "tests", "Shared", "TemplatesTesting", "data", "nuget8.config");
        if (!File.Exists(nugetConfig))
        {
            missing.Add($"NuGet config template ({nugetConfig})");
        }

        var hint = !string.IsNullOrEmpty(artifactsRoot)
            ? $"Looking in ASPIRE_ARTIFACTS_ROOT={artifactsRoot}"
            : "Set ASPIRE_ARTIFACTS_ROOT to point to a repo with built artifacts, or run: ./build.sh --bundle --build-extension --pack";

        return missing.Count > 0
            ? $"Missing build artifacts. {hint}\n  - {string.Join("\n  - ", missing)}"
            : "All artifacts present.";
    }

    private static string? FindCliPublishDirectory(string repoRoot)
    {
        // Search both Aspire.Cli.Tool (framework-dependent from localhive.sh)
        // and Aspire.Cli (native AOT from build.sh --bundle).
        // Prefer Release to match localhive.sh package versions.
        foreach (var dirName in new[] { "Aspire.Cli.Tool", "Aspire.Cli" })
        {
            var cliBaseDir = Path.Combine(repoRoot, "artifacts", "bin", dirName);
            if (!Directory.Exists(cliBaseDir))
            {
                continue;
            }

            var matches = Directory.GetFiles(cliBaseDir, "aspire", SearchOption.AllDirectories)
                .Where(f => !f.Contains("win-") && !f.Contains("osx-"))
                .OrderByDescending(f => f.Contains("Release")) // prefer Release config
                .ThenByDescending(f => f.Contains("publish"))  // then prefer publish dirs
                .ToArray();

            if (matches.Length > 0)
            {
                return Path.GetDirectoryName(matches[0]);
            }
        }

        return null;
    }

    private static string? FindPackagesDirectory(string repoRoot)
    {
        // Prefer Release (localhive.sh produces properly versioned prerelease packages)
        foreach (var config in new[] { "Release", "Debug" })
        {
            var dir = Path.Combine(repoRoot, "artifacts", "packages", config, "Shipping");
            if (Directory.Exists(dir) && Directory.GetFiles(dir, "*.nupkg").Length > 0)
            {
                return dir;
            }
        }

        return null;
    }

    private static string? FindVsixPath(string repoRoot)
    {
        foreach (var config in new[] { "Debug", "Release" })
        {
            var dir = Path.Combine(repoRoot, "artifacts", "packages", config, "vscode");
            if (Directory.Exists(dir))
            {
                var vsixFiles = Directory.GetFiles(dir, "aspire-vscode-*.vsix");
                if (vsixFiles.Length > 0)
                {
                    return vsixFiles[0];
                }
            }
        }

        return null;
    }
}
