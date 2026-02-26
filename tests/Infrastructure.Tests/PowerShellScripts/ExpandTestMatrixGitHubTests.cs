// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Tests for eng/scripts/expand-test-matrix-github.ps1
/// </summary>
public class ExpandTestMatrixGitHubTests : IDisposable
{
    private readonly TestTempDirectory _tempDir = new();
    private readonly string _scriptPath;
    private readonly ITestOutputHelper _output;

    public ExpandTestMatrixGitHubTests(ITestOutputHelper output)
    {
        _output = output;
        _scriptPath = Path.Combine(FindRepoRoot(), "eng", "scripts", "expand-test-matrix-github.ps1");
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task MapsWindowsToRunner()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["windows"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful("expand-test-matrix-github.ps1 failed");

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        Assert.Single(expanded.Include);
        Assert.Equal("windows-latest", expanded.Include[0].RunsOn);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task MapsLinuxToRunner()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        Assert.Single(expanded.Include);
        Assert.Equal("ubuntu-latest", expanded.Include[0].RunsOn);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task MapsMacOSToRunner()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["macos"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        Assert.Single(expanded.Include);
        Assert.Equal("macos-latest", expanded.Include[0].RunsOn);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task ExpandsMultipleOSes()
    {
        // Arrange - One entry with all 3 OSes
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "MultiOSProject",
            projectName: "MultiOSProject",
            testProjectPath: "tests/MultiOSProject/MultiOSProject.csproj",
            supportedOSes: ["windows", "linux", "macos"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        // One entry * 3 OSes = 3 expanded entries
        Assert.Equal(3, expanded.Include.Length);
        Assert.Contains(expanded.Include, e => e.RunsOn == "windows-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "ubuntu-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "macos-latest");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task GeneratesIncludeFormat()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        // Verify the JSON structure has { "include": [...] }
        // Note: PowerShell may serialize single-element arrays as objects
        var json = File.ReadAllText(outputPrefix + "_no_nugets.json");
        var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("include", out var include));
        Assert.True(
            include.ValueKind == JsonValueKind.Array || include.ValueKind == JsonValueKind.Object,
            $"Expected include to be Array or Object, got {include.ValueKind}");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RemovesSupportedOSes()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["windows"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        // Check that supportedOSes is removed from expanded entries
        var json = File.ReadAllText(outputPrefix + "_no_nugets.json");
        var document = JsonDocument.Parse(json);
        var include = document.RootElement.GetProperty("include");

        // PowerShell may serialize single-element array as object
        var firstEntry = include.ValueKind == JsonValueKind.Array
            ? include.EnumerateArray().First()
            : include;

        Assert.False(firstEntry.TryGetProperty("supportedOSes", out _),
            "supportedOSes should be removed from expanded entries");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task AddsRunsOnProperty()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "TestProject",
            projectName: "TestProject",
            testProjectPath: "tests/TestProject/TestProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var json = File.ReadAllText(outputPrefix + "_no_nugets.json");
        var document = JsonDocument.Parse(json);
        var include = document.RootElement.GetProperty("include");

        // PowerShell may serialize single-element array as object
        var firstEntry = include.ValueKind == JsonValueKind.Array
            ? include.EnumerateArray().First()
            : include;

        Assert.True(firstEntry.TryGetProperty("runs-on", out var runsOn),
            "runs-on should be added to expanded entries");
        Assert.Equal("ubuntu-latest", runsOn.GetString());
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task SplitsRequiresNugetsIntoSeparateMatrix()
    {
        // Arrange
        var nugetsEntry = TestDataBuilder.CreateMatrixEntry(
            name: "NugetsProject",
            projectName: "NugetsProject",
            testProjectPath: "tests/NugetsProject/NugetsProject.csproj",
            requiresNugets: true,
            supportedOSes: ["linux"]);

        var noNugetsEntry = TestDataBuilder.CreateMatrixEntry(
            name: "NoNugetsProject",
            projectName: "NoNugetsProject",
            testProjectPath: "tests/NoNugetsProject/NoNugetsProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(
            canonicalMatrix,
            tests: [nugetsEntry, noNugetsEntry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var noNugetsMatrix = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var nugetsMatrix = ParseGitHubMatrix(outputPrefix + "_requires_nugets.json");

        Assert.Single(noNugetsMatrix.Include);
        Assert.Equal("NoNugetsProject", noNugetsMatrix.Include[0].ProjectName);

        Assert.Single(nugetsMatrix.Include);
        Assert.Equal("NugetsProject", nugetsMatrix.Include[0].ProjectName);
        Assert.True(nugetsMatrix.Include[0].RequiresNugets);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PreservesAllEntryProperties()
    {
        // Arrange
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "FullProject",
            projectName: "FullProject",
            testProjectPath: "tests/FullProject/FullProject.csproj",
            type: "collection",
            shortname: "Full",
            workitemprefix: "FullProject_Part",
            collection: "MyPartition",
            extraTestArgs: "--filter-trait \"Partition=MyPartition\"",
            testSessionTimeout: "30m",
            testHangTimeout: "15m",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var expandedEntry = Assert.Single(expanded.Include);

        Assert.Equal("FullProject", expandedEntry.ProjectName);
        Assert.Equal("FullProject", expandedEntry.Name);
        Assert.Equal("collection", expandedEntry.Type);
        Assert.Equal("Full", expandedEntry.Shortname);
        Assert.Equal("FullProject_Part", expandedEntry.Workitemprefix);
        Assert.Equal("MyPartition", expandedEntry.Collection);
        Assert.Equal("--filter-trait \"Partition=MyPartition\"", expandedEntry.ExtraTestArgs);
        Assert.Equal("30m", expandedEntry.TestSessionTimeout);
        Assert.Equal("15m", expandedEntry.TestHangTimeout);
        Assert.Equal("ubuntu-latest", expandedEntry.RunsOn);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task HandlesEmptyMatrix()
    {
        // Arrange
        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        Assert.Empty(expanded.Include);

        var nugetsExpanded = ParseGitHubMatrix(outputPrefix + "_requires_nugets.json");
        Assert.Empty(nugetsExpanded.Include);

        var cliArchiveExpanded = ParseGitHubMatrix(outputPrefix + "_requires_cli_archive.json");
        Assert.Empty(cliArchiveExpanded.Include);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task FailsWhenCanonicalMatrixNotFound()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDir.Path, "does-not-exist.json");
        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(nonExistentFile, outputMatrixFile: outputPrefix + ".json");

        // Assert
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("not found", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task DefaultsToAllOSesWhenSupportedOSesEmpty()
    {
        // Arrange - Entry with empty supportedOSes array
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "NoOsSpecified",
            projectName: "NoOsSpecified",
            testProjectPath: "tests/NoOsSpecified/NoOsSpecified.csproj",
            supportedOSes: []); // Empty array

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        // Should default to all 3 OSes
        Assert.Equal(3, expanded.Include.Length);
        Assert.Contains(expanded.Include, e => e.RunsOn == "windows-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "ubuntu-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "macos-latest");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task WarnsOnInvalidOS()
    {
        // Arrange - Entry with invalid OS name
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "BadOs",
            projectName: "BadOs",
            testProjectPath: "tests/BadOs/BadOs.csproj",
            supportedOSes: ["linux", "invalid-os"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        // Should warn about the invalid OS
        Assert.Contains("invalid", result.Output.ToLowerInvariant());

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        // Should only have the valid linux entry
        Assert.Single(expanded.Include);
        Assert.Equal("ubuntu-latest", expanded.Include[0].RunsOn);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task HandlesOSNamesWithDifferentCasing()
    {
        // Arrange - OS names should be case-insensitive
        var entry = TestDataBuilder.CreateMatrixEntry(
            name: "CasedOs",
            projectName: "CasedOs",
            testProjectPath: "tests/CasedOs/CasedOs.csproj",
            supportedOSes: ["WINDOWS", "Linux", "MacOS"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [entry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        Assert.Equal(3, expanded.Include.Length);
        Assert.Contains(expanded.Include, e => e.RunsOn == "windows-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "ubuntu-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "macos-latest");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task OverflowsEntriesBeyondThreshold()
    {
        // Arrange - Create more than 250 entries (overflow threshold) for no-nugets category
        // Each entry with 1 OS = 1 expanded entry, so 260 entries should split 250/10
        var entries = Enumerable.Range(1, 260).Select(i =>
            TestDataBuilder.CreateMatrixEntry(
                name: $"Project{i}",
                projectName: $"Project{i}",
                testProjectPath: $"tests/Project{i}/Project{i}.csproj",
                supportedOSes: ["linux"])).ToArray();

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: entries);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var primary = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var overflow = ParseGitHubMatrix(outputPrefix + "_no_nugets_overflow.json");

        Assert.Equal(250, primary.Include.Length);
        Assert.Equal(10, overflow.Include.Length);

        // Verify no duplicates - all 260 projects should be accounted for
        var allNames = primary.Include.Concat(overflow.Include).Select(e => e.ProjectName).ToHashSet();
        Assert.Equal(260, allNames.Count);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task EmptyOverflowWhenBelowThreshold()
    {
        // Arrange - 10 entries, well below 250 threshold
        var entries = Enumerable.Range(1, 10).Select(i =>
            TestDataBuilder.CreateMatrixEntry(
                name: $"Project{i}",
                projectName: $"Project{i}",
                testProjectPath: $"tests/Project{i}/Project{i}.csproj",
                supportedOSes: ["linux"])).ToArray();

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: entries);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var primary = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var overflow = ParseGitHubMatrix(outputPrefix + "_no_nugets_overflow.json");

        Assert.Equal(10, primary.Include.Length);
        Assert.Empty(overflow.Include);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task SplitTestsGoToNoNugetsCategory()
    {
        // Arrange - Split tests (splitTests=true) should be grouped with no-nugets, not separate
        var splitEntry = TestDataBuilder.CreateMatrixEntry(
            name: "SplitProject",
            projectName: "SplitProject",
            testProjectPath: "tests/SplitProject/SplitProject.csproj",
            supportedOSes: ["linux"]);
        // Mark as split test by adding the splitTests property
        splitEntry.SplitTests = true;

        var regularEntry = TestDataBuilder.CreateMatrixEntry(
            name: "RegularProject",
            projectName: "RegularProject",
            testProjectPath: "tests/RegularProject/RegularProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(canonicalMatrix, tests: [splitEntry, regularEntry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var noNugets = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        // Both split and regular entries should be in the no-nugets matrix
        Assert.Equal(2, noNugets.Include.Length);
        Assert.Contains(noNugets.Include, e => e.ProjectName == "SplitProject");
        Assert.Contains(noNugets.Include, e => e.ProjectName == "RegularProject");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task SplitsCliArchiveIntoSeparateCategory()
    {
        // Arrange - CLI archive tests get their own category separate from requires-nugets
        var cliArchiveEntry = TestDataBuilder.CreateMatrixEntry(
            name: "CliE2E",
            projectName: "CliE2E",
            testProjectPath: "tests/CliE2E/CliE2E.csproj",
            requiresNugets: true,
            requiresCliArchive: true,
            supportedOSes: ["linux"]);

        var nugetsEntry = TestDataBuilder.CreateMatrixEntry(
            name: "NugetsProject",
            projectName: "NugetsProject",
            testProjectPath: "tests/NugetsProject/NugetsProject.csproj",
            requiresNugets: true,
            supportedOSes: ["linux"]);

        var noNugetsEntry = TestDataBuilder.CreateMatrixEntry(
            name: "RegularProject",
            projectName: "RegularProject",
            testProjectPath: "tests/RegularProject/RegularProject.csproj",
            supportedOSes: ["linux"]);

        var canonicalMatrix = Path.Combine(_tempDir.Path, "canonical.json");
        TestDataBuilder.CreateCanonicalMatrixJson(
            canonicalMatrix,
            tests: [cliArchiveEntry, nugetsEntry, noNugetsEntry]);

        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputPrefix + ".json");

        // Assert
        result.EnsureSuccessful();

        var noNugets = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var nugets = ParseGitHubMatrix(outputPrefix + "_requires_nugets.json");
        var cliArchive = ParseGitHubMatrix(outputPrefix + "_requires_cli_archive.json");

        Assert.Single(noNugets.Include);
        Assert.Equal("RegularProject", noNugets.Include[0].ProjectName);

        Assert.Single(nugets.Include);
        Assert.Equal("NugetsProject", nugets.Include[0].ProjectName);

        Assert.Single(cliArchive.Include);
        Assert.Equal("CliE2E", cliArchive.Include[0].ProjectName);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task FullPipeline_SplitTestsExpandPerOS()
    {
        // This test validates the full pipeline: build-test-matrix → expand-test-matrix-github
        // with a realistic multi-project scenario to catch regressions where OS coverage
        // is silently dropped (e.g., a split test project losing multi-OS expansion).
        var artifactsDir = Path.Combine(_tempDir.Path, "artifacts");
        Directory.CreateDirectory(artifactsDir);

        // Regular project on all 3 OSes
        TestDataBuilder.CreateTestsMetadataJson(
            Path.Combine(artifactsDir, "RegularProject.tests-metadata.json"),
            projectName: "RegularProject",
            testProjectPath: "tests/RegularProject/RegularProject.csproj",
            shortName: "Regular");

        // Split test project on all 3 OSes (like Templates)
        TestDataBuilder.CreateSplitTestsMetadataJson(
            Path.Combine(artifactsDir, "SplitMultiOS.tests-metadata.json"),
            projectName: "SplitMultiOS",
            testProjectPath: "tests/SplitMultiOS/SplitMultiOS.csproj",
            shortName: "SplitMultiOS",
            supportedOSes: ["windows", "linux", "macos"]);

        TestDataBuilder.CreateClassBasedPartitionsJson(
            Path.Combine(artifactsDir, "SplitMultiOS.tests-partitions.json"),
            "Namespace.ClassA", "Namespace.ClassB");

        // Linux-only project requiring NuGets (like EndToEnd)
        TestDataBuilder.CreateTestsMetadataJson(
            Path.Combine(artifactsDir, "LinuxE2E.tests-metadata.json"),
            projectName: "LinuxE2E",
            testProjectPath: "tests/LinuxE2E/LinuxE2E.csproj",
            shortName: "LinuxE2E",
            requiresNugets: true,
            supportedOSes: ["linux"]);

        // Linux-only project requiring CLI archive (like Cli.EndToEnd.Tests)
        TestDataBuilder.CreateTestsMetadataJson(
            Path.Combine(artifactsDir, "CliE2E.tests-metadata.json"),
            projectName: "CliE2E",
            testProjectPath: "tests/CliE2E/CliE2E.csproj",
            shortName: "CliE2E",
            requiresNugets: true,
            requiresCliArchive: true,
            supportedOSes: ["linux"]);

        // Run build-test-matrix.ps1
        var buildMatrixScript = Path.Combine(FindRepoRoot(), "eng", "scripts", "build-test-matrix.ps1");
        var canonicalFile = Path.Combine(_tempDir.Path, "canonical.json");

        using var buildCmd = new PowerShellCommand(buildMatrixScript, _output)
            .WithTimeout(TimeSpan.FromMinutes(2));
        var buildResult = await buildCmd.ExecuteAsync(
            "-ArtifactsDir", $"\"{artifactsDir}\"",
            "-OutputMatrixFile", $"\"{canonicalFile}\"");
        buildResult.EnsureSuccessful("build-test-matrix.ps1 failed");

        // Run expand-test-matrix-github.ps1
        var outputPrefix = Path.Combine(_tempDir.Path, "expanded");
        var expandResult = await RunScript(canonicalFile, outputMatrixFile: outputPrefix + ".json");
        expandResult.EnsureSuccessful("expand-test-matrix-github.ps1 failed");

        // Read all output matrices
        var noNugets = ParseGitHubMatrix(outputPrefix + "_no_nugets.json");
        var noNugetsOverflow = ParseGitHubMatrix(outputPrefix + "_no_nugets_overflow.json");
        var nugetsMatrix = ParseGitHubMatrix(outputPrefix + "_requires_nugets.json");
        var cliArchiveMatrix = ParseGitHubMatrix(outputPrefix + "_requires_cli_archive.json");

        // Combine no-nugets primary + overflow for full validation
        var allNoNugets = noNugets.Include.Concat(noNugetsOverflow.Include).ToArray();

        // Regular project: 1 project × 3 OSes = 3
        var regularEntries = allNoNugets.Where(e => e.ProjectName == "RegularProject").ToArray();
        Assert.Equal(3, regularEntries.Length);

        // Split project: 2 classes × 3 OSes = 6 (all in no-nugets since splitTests merged)
        var splitEntries = allNoNugets.Where(e => e.ProjectName == "SplitMultiOS").ToArray();
        Assert.Equal(6, splitEntries.Length);
        Assert.Equal(2, splitEntries.Count(e => e.RunsOn == "ubuntu-latest"));
        Assert.Equal(2, splitEntries.Count(e => e.RunsOn == "windows-latest"));
        Assert.Equal(2, splitEntries.Count(e => e.RunsOn == "macos-latest"));

        // Linux-only E2E: 1 project × 1 OS = 1, in requires-nugets matrix
        var e2eEntries = nugetsMatrix.Include.Where(e => e.ProjectName == "LinuxE2E").ToArray();
        Assert.Single(e2eEntries);
        Assert.Equal("ubuntu-latest", e2eEntries[0].RunsOn);
        Assert.True(e2eEntries[0].RequiresNugets);

        // CLI E2E: 1 project × 1 OS = 1, in requires-cli-archive matrix
        var cliE2eEntries = cliArchiveMatrix.Include.Where(e => e.ProjectName == "CliE2E").ToArray();
        Assert.Single(cliE2eEntries);
        Assert.Equal("ubuntu-latest", cliE2eEntries[0].RunsOn);
        Assert.True(cliE2eEntries[0].RequiresCliArchive);

        // Total no-nugets: 3 + 6 = 9, Total nugets: 1, Total cli-archive: 1
        Assert.Equal(9, allNoNugets.Length);
        Assert.Single(nugetsMatrix.Include);
        Assert.Single(cliArchiveMatrix.Include);
    }

    private async Task<CommandResult> RunScript(
        string canonicalMatrixFile,
        string? outputMatrixFile = null)
    {
        using var cmd = new PowerShellCommand(_scriptPath, _output)
            .WithTimeout(TimeSpan.FromMinutes(2));

        var args = new List<string>
        {
            "-CanonicalMatrixFile", $"\"{canonicalMatrixFile}\""
        };

        if (!string.IsNullOrEmpty(outputMatrixFile))
        {
            args.Add("-OutputMatrixFile");
            args.Add($"\"{outputMatrixFile}\"");
        }

        return await cmd.ExecuteAsync(args.ToArray());
    }

    private static GitHubActionsMatrix ParseGitHubMatrix(string path)
    {
        var json = File.ReadAllText(path);
        var document = JsonDocument.Parse(json);
        var include = document.RootElement.GetProperty("include");

        // PowerShell's ConvertTo-Json serializes single-element arrays as objects
        // Handle both cases
        ExpandedMatrixEntry[] entries;
        if (include.ValueKind == JsonValueKind.Array)
        {
            entries = JsonSerializer.Deserialize<ExpandedMatrixEntry[]>(include.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        else if (include.ValueKind == JsonValueKind.Object)
        {
            var singleEntry = JsonSerializer.Deserialize<ExpandedMatrixEntry>(include.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            entries = singleEntry is not null ? [singleEntry] : [];
        }
        else
        {
            entries = [];
        }

        return new GitHubActionsMatrix { Include = entries };
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Aspire.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repository root");
    }
}
