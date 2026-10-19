// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.SelectTests;

// Shadow-only: this record does not change test selection, producer gates, or platform coverage.
internal sealed class ArtifactDemand
{
    private static readonly string[] s_allRids = ["linux-arm64", "linux-x64", "osx-arm64", "osx-x64", "win-arm64", "win-x64"];

    internal bool FullDogfood { get; private set; }
    internal bool CommonFeed { get; private set; }
    internal bool Vsix { get; private set; }
    internal SortedSet<string> RidPackages { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> CliArchives { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> StandaloneDashboard { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> Installers { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> Reasons { get; } = new(StringComparer.Ordinal);

    internal static void WriteIfRequested(
        SelectionResult selection,
        IReadOnlyCollection<string> rawChangedFiles,
        AffectedResult affected,
        bool graphAvailable)
    {
        var matrixPath = Environment.GetEnvironmentVariable("SELECT_TESTS_ARTIFACT_DEMAND_MATRIX");
        var outputPath = Environment.GetEnvironmentVariable("SELECT_TESTS_ARTIFACT_DEMAND_FILE");
        if (string.IsNullOrEmpty(matrixPath) && string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        if (string.IsNullOrEmpty(matrixPath) || string.IsNullOrEmpty(outputPath))
        {
            throw new InvalidOperationException("Experimental artifact demand requires both SELECT_TESTS_ARTIFACT_DEMAND_MATRIX and SELECT_TESTS_ARTIFACT_DEMAND_FILE.");
        }

        if (Path.GetFullPath(matrixPath) == Path.GetFullPath(outputPath))
        {
            throw new InvalidOperationException("Artifact-demand output must not overwrite its matrix input.");
        }

        using var matrix = JsonDocument.Parse(File.ReadAllText(matrixPath));
        var demand = Compute(selection, rawChangedFiles, affected, graphAvailable, matrix.RootElement);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, demand.ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Experimental artifact demand (shadow only): {outputPath}");
    }

    internal static ArtifactDemand Compute(
        SelectionResult selection,
        IReadOnlyCollection<string> rawChangedFiles,
        AffectedResult affected,
        bool graphAvailable,
        JsonElement matrix)
    {
        // Expanded all_tests shape, from expand-test-matrix-github.ps1:
        // {"include":[{"projectName":"Aspire.Templates.Tests","runs-on":"macos-latest",
        //   "properties":{"requiresNugets":true,"requiresCliArchive":false}}]}
        // Missing or mistyped metadata must not be interpreted as a package-free consumer.
        var entries = matrix.GetProperty("include");
        if (entries.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Artifact-demand matrix 'include' must be an array.");
        }

        var demand = new ArtifactDemand();
        var representedTests = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries.EnumerateArray())
        {
            var project = entry.GetProperty("projectName").GetString()
                ?? throw new InvalidOperationException("Artifact-demand matrix projectName must be a string.");
            if (!selection.SelectsAll && !selection.TestProjects.Contains(project))
            {
                continue;
            }

            representedTests.Add(project);
            if (!entry.TryGetProperty("properties", out var properties) ||
                !properties.TryGetProperty("requiresNugets", out var packages) ||
                !properties.TryGetProperty("requiresCliArchive", out var archive))
            {
                demand.RequireFull($"Missing dependency metadata: {project}");
                continue;
            }

            var requiresPackages = packages.GetBoolean();
            var requiresArchive = archive.GetBoolean();
            demand.Reasons.Add($"Test consumer: {project}");
            if (!requiresPackages && !requiresArchive)
            {
                continue;
            }

            var runner = entry.GetProperty("runs-on").GetString();
            // Explicit public runner identities, not filename/OS heuristics. New labels fail safe.
            // https://docs.github.com/en/actions/reference/runners/github-hosted-runners
            var rid = runner switch
            {
                "ubuntu-latest" or "ubuntu-24.04" => "linux-x64",
                "ubuntu-24.04-arm" => "linux-arm64",
                "windows-latest" or "windows-2022" or "windows-2025" => "win-x64",
                "windows-11-arm" => "win-arm64",
                "macos-latest" or "macos-15" => "osx-arm64",
                "macos-15-intel" => "osx-x64",
                _ => null
            };
            if (rid is null)
            {
                demand.RequireFull($"Unknown package consumer runner: {project} ({runner})");
                continue;
            }

            if (requiresPackages)
            {
                demand.RequirePackages([rid]);
            }
            if (requiresArchive)
            {
                demand.CliArchives.Add(rid);
            }
        }

        foreach (var missing in selection.TestProjects.Except(representedTests, StringComparer.Ordinal))
        {
            // A restricted/partial matrix cannot prove that an omitted selected consumer needs nothing.
            demand.RequireFull($"Selected test absent from expanded matrix: {missing}");
        }

        foreach (var job in selection.Jobs)
        {
            demand.Reasons.Add($"Job consumer: {job}");
            switch (job)
            {
                case "job:polyglot":
                    demand.RequireCli(["linux-x64"]);
                    break;
                case "job:extension-e2e":
                    demand.Vsix = true;
                    demand.RequireCli(["linux-x64", "win-x64"]);
                    break;
                case "job:extension-unit":
                    demand.Vsix = true;
                    break;
                case "job:winget-installer":
                    demand.Installers.Add("winget");
                    demand.CommonFeed = true;
                    demand.CliArchives.UnionWith(["win-arm64", "win-x64"]);
                    break;
                case "job:homebrew-installer":
                    demand.Installers.Add("homebrew");
                    demand.CommonFeed = true;
                    demand.CliArchives.UnionWith(["osx-arm64", "osx-x64"]);
                    break;
                case "job:cli-starter-validation":
                    demand.RequireCli(s_allRids);
                    break;
                case "job:native-dashboard-validation":
                    demand.StandaloneDashboard.UnionWith(s_allRids);
                    break;
                case "job:typescript-sdk":
                case "job:typescript-api-compat":
                case "job:nix-package":
                case "job:deployment-e2e":
                    // These do not download PR package/native artifacts; deployment is advisory here.
                    break;
                default:
                    demand.RequireFull($"Unknown job consumer: {job}");
                    break;
            }
        }

        if (selection.SelectsAll)
        {
            demand.RequireFull($"Full selection: {selection.EscalationReason ?? "ALL"}");
        }
        else if (!graphAvailable)
        {
            demand.RequireFull("Shipping impact unknown: Layer 1 unavailable");
        }
        else if (affected.AffectedProjects.Except(affected.AffectedTestProjects, StringComparer.Ordinal).Any())
        {
            demand.RequireFull("Production project affected");
        }

        if (!selection.SelectsAll && rawChangedFiles.Count == 0)
        {
            demand.RequireFull("Shipping impact unknown: no changed inputs");
        }

        foreach (var path in rawChangedFiles)
        {
            // These paths are ignored by test selection only because native producers currently run
            // unconditionally. Preserve producer-side verification even when no test is selected.
            if (path is "eng/scripts/verify-cli-npm-package.ps1" or "eng/scripts/verify-cli-tool-nupkg.ps1" or
                "eng/scripts/test-native-terminalhost.ps1" ||
                path.StartsWith("eng/scripts/tray-registration-control/", StringComparison.Ordinal))
            {
                demand.RequireCli(s_allRids);
                demand.StandaloneDashboard.UnionWith(s_allRids);
                demand.Reasons.Add($"Native producer verification: {path}");
            }
            else if (!path.StartsWith("tests/", StringComparison.Ordinal) || !affected.AttributedPaths.Contains(path))
            {
                // Use the RAW diff, not the test prefilter: packed READMEs and template content can
                // affect dogfood. This also preserves shipping deletes/rename old paths and unknowns.
                demand.RequireFull($"Shipping or unattributed input: {path}");
            }
        }

        return demand;
    }

    internal JsonObject ToJson() => new()
    {
        ["schemaVersion"] = 1,
        ["experimental"] = true,
        ["shadowOnly"] = true,
        ["fullDogfood"] = FullDogfood,
        ["commonFeed"] = CommonFeed,
        ["ridPackages"] = ToArray(RidPackages),
        ["cliArchives"] = ToArray(CliArchives),
        ["standaloneDashboard"] = ToArray(StandaloneDashboard),
        ["vsix"] = Vsix,
        ["installers"] = ToArray(Installers),
        ["reasons"] = ToArray(Reasons)
    };

    private void RequirePackages(IEnumerable<string> rids)
    {
        CommonFeed = true;
        RidPackages.UnionWith(rids);
    }

    private void RequireCli(IEnumerable<string> rids)
    {
        RequirePackages(rids);
        CliArchives.UnionWith(rids);
    }

    private void RequireFull(string reason)
    {
        FullDogfood = true;
        RequireCli(s_allRids);
        StandaloneDashboard.UnionWith(s_allRids);
        Vsix = true;
        Installers.UnionWith(["homebrew", "winget"]);
        Reasons.Add(reason);
    }

    private static JsonArray ToArray(IEnumerable<string> values) =>
        new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
}
