// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.SelectTests;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests.TestTriggerMap;

public sealed class InfrastructureTestInputRoutingTests(ITestOutputHelper output)
{
    private const string InfrastructureTestPaths = "eng/github-ci/ci-infrastructure-test-paths.txt";
    private const string ClassifierScript = "eng/github-ci/classify-ci-changes.sh";

    public static TheoryData<string, string[], bool, bool, bool> ClassificationCases => new()
    {
        { "no changed files", [], false, true, false },
        { "Infrastructure.Tests input only", ["eng/github-ci/test-trigger-map.yml"], true, false, false },
        { "skipped Infrastructure.Tests input only", [".github/workflows/auto-rerun-transient-ci-failures.js"], true, false, false },
        { "documentation only", ["docs/README.md"], false, true, false },
        { "pipeline only", ["eng/pipelines/release-publish-nuget.yml"], false, true, false },
        { "generated agentic workflow", [".github/workflows/analyze-ci-failure.lock.yml"], false, true, false },
        {
            "Infrastructure.Tests input with documentation",
            ["eng/github-ci/test-trigger-map.yml", "docs/README.md"],
            true,
            false,
            false
        },
        {
            "Infrastructure.Tests input with source",
            ["eng/github-ci/test-trigger-map.yml", "src/Aspire.Hosting/ApplicationModel/Resource.cs"],
            true,
            false,
            true
        },
    };

    [Theory]
    [MemberData(nameof(ClassificationCases))]
    [RequiresTools(["bash", "jq"])]
    public async Task ChangedFilesProduceExpectedCiAndStabilizationDecisions(
        string _,
        string[] changedFiles,
        bool expectedHasInfrastructureTestInputs,
        bool expectedSkipWorkflow,
        bool expectedStabilizationRequired)
    {
        var classification = await ClassifyAsync(changedFiles);

        Assert.Equal(expectedHasInfrastructureTestInputs, classification.HasInfrastructureTestInputs);
        Assert.Equal(expectedSkipWorkflow, classification.SkipWorkflow);
        Assert.Equal(expectedStabilizationRequired, classification.StabilizationRequired);
    }

    [Fact]
    [RequiresTools(["bash", "jq"])]
    public async Task EveryInfrastructureTestInputSelectsInfrastructureTestsWithoutStabilization()
    {
        var paths = ReadPatterns(InfrastructureTestPaths);
        Assert.NotEmpty(paths);

        var classification = await ClassifyAsync(paths);

        Assert.True(classification.HasInfrastructureTestInputs);
        Assert.False(classification.SkipWorkflow);
        Assert.False(classification.StabilizationRequired);

        foreach (var path in paths)
        {
            var selection = SelectWithRealMap(path);

            Assert.False(selection.SelectsAll, path);
            Assert.Equal(["Infrastructure.Tests"], selection.TestProjects);
        }
    }

    [Theory]
    [InlineData("docs/README.md")]
    [InlineData("docs/specs/npm-cli-package.md")]
    [InlineData("eng/pipelines/release-publish-nuget.yml")]
    [InlineData(".github/workflows/backport.yml")]
    [InlineData(".github/workflows/auto-rerun-transient-ci-failures.yml")]
    public void UnmappedSkippedInputsSelectNoNormalCiTargets(string path)
    {
        var selection = SelectWithRealMap(path);

        Assert.False(selection.SelectsAll);
        Assert.Empty(selection.TestProjects);
        Assert.Empty(selection.Jobs);
    }

    private async Task<CiClassification> ClassifyAsync(IReadOnlyList<string> changedFiles)
    {
        var skipPatterns = ReadPatterns("eng/github-ci/ci-skip-entirely-patterns.txt");
        var infrastructureTestPatterns = ReadPatterns(InfrastructureTestPaths);
        var skippableFiles = changedFiles.Where(path => MatchesAny(skipPatterns, path)).ToArray();
        var infrastructureTestFiles = changedFiles.Where(path => MatchesAny(infrastructureTestPatterns, path)).ToArray();
        var result = await ProcessRunner.RunAsync(
            output,
            "bash",
            [
                Path.Combine(RepoRoot.Path, ClassifierScript),
                JsonSerializer.Serialize(changedFiles),
                JsonSerializer.Serialize(skippableFiles),
                JsonSerializer.Serialize(infrastructureTestFiles),
            ],
            RepoRoot.Path);

        Assert.Equal(0, result.ExitCode);
        var outputs = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

        return new CiClassification(
            bool.Parse(outputs["has_infrastructure_test_inputs"]),
            bool.Parse(outputs["skip_workflow"]),
            bool.Parse(outputs["stabilization_required"]));
    }

    private static IReadOnlyList<string> ReadPatterns(string relativePath)
        => File.ReadAllLines(Path.Combine(RepoRoot.Path, relativePath))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();

    private static bool MatchesAny(IEnumerable<string> patterns, string path)
        => patterns.Any(pattern => TestTriggerMap.GlobMatches(pattern, path));

    private static SelectionResult SelectWithRealMap(params string[] paths)
    {
        var projectPaths = Regex.Matches(
                File.ReadAllText(Path.Combine(RepoRoot.Path, "Aspire.slnx")),
                "Path=\"([^\"]+\\.csproj)\"")
            .Select(match => match.Groups[1].Value.Replace('\\', '/'))
            .ToList();
        var testProjects = projectPaths
            .Where(projectPath => projectPath.StartsWith("tests/", StringComparison.Ordinal))
            .Select(projectPath => Path.GetFileNameWithoutExtension(projectPath)!)
            .Where(name => name.EndsWith(".Tests", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var projectDirectories = projectPaths
            .Select(projectPath => Path.GetDirectoryName(projectPath)!.Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);
        var mapPath = Path.Combine(RepoRoot.Path, "eng", "github-ci", "test-trigger-map.yml");
        var map = TriggerMap.Load(mapPath);
        var filter = ChangedFileFilter.Create(RepoRoot.Path, map.Prefilter);
        var filteredPaths = paths.Where(path => !filter.IsExcluded(path)).ToArray();
        var selector = new TestSelector(
            mapPath,
            testProjects,
            projectDirectories,
            new HashSet<string>(StringComparer.Ordinal));

        return selector.Select(filteredPaths, [], new SelectorOptions());
    }

    private readonly record struct CiClassification(
        bool HasInfrastructureTestInputs,
        bool SkipWorkflow,
        bool StabilizationRequired);
}
