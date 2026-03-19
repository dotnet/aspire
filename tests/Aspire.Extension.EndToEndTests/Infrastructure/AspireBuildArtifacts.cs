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
    public static AspireBuildArtifacts? Detect(string repoRoot)
    {
        // 1. CLI binary: artifacts/bin/Aspire.Cli/*/net*/linux-x64/publish/aspire
        //    Same detection logic as CliE2ETestHelpers.FindLocalCliBinary()
        var cliPublishDir = FindCliPublishDirectory(repoRoot);

        // 2. NuGet packages: artifacts/packages/Debug/Shipping/ (or Release)
        var packagesDir = FindPackagesDirectory(repoRoot);

        // 3. VSIX: artifacts/packages/Debug/vscode/aspire-vscode-*.vsix (or Release)
        var vsixPath = FindVsixPath(repoRoot);

        // 4. NuGet config template
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
        var missing = new List<string>();

        if (FindCliPublishDirectory(repoRoot) is null)
        {
            missing.Add("CLI native binary (run: ./build.sh --bundle)");
        }

        if (FindPackagesDirectory(repoRoot) is null)
        {
            missing.Add("NuGet packages (run: ./build.sh --pack)");
        }

        if (FindVsixPath(repoRoot) is null)
        {
            missing.Add("VS Code extension VSIX (run: ./build.sh --build-extension)");
        }

        var nugetConfig = Path.Combine(repoRoot, "tests", "Shared", "TemplatesTesting", "data", "nuget8.config");
        if (!File.Exists(nugetConfig))
        {
            missing.Add($"NuGet config template ({nugetConfig})");
        }

        return missing.Count > 0
            ? $"Missing build artifacts. Run: ./build.sh --bundle --build-extension --pack\n  - {string.Join("\n  - ", missing)}"
            : "All artifacts present.";
    }

    private static string? FindCliPublishDirectory(string repoRoot)
    {
        var cliBaseDir = Path.Combine(repoRoot, "artifacts", "bin", "Aspire.Cli");
        if (!Directory.Exists(cliBaseDir))
        {
            return null;
        }

        var matches = Directory.GetFiles(cliBaseDir, "aspire", SearchOption.AllDirectories)
            .Where(f => f.Contains("linux-x64") && f.Contains("publish"))
            .ToArray();

        return matches.Length > 0 ? Path.GetDirectoryName(matches[0]) : null;
    }

    private static string? FindPackagesDirectory(string repoRoot)
    {
        // Check Debug first, then Release
        foreach (var config in new[] { "Debug", "Release" })
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
