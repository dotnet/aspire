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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful("expand-test-matrix-github.ps1 failed");

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        // Verify the JSON structure has { "include": [...] }
        // Note: PowerShell may serialize single-element arrays as objects
        var json = File.ReadAllText(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        // Check that supportedOSes is removed from expanded entries
        var json = File.ReadAllText(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var json = File.ReadAllText(outputFile);
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
    public async Task PreservesRequiresNugetsProperty()
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var combinedMatrix = ParseGitHubMatrix(outputFile);

        // Both entries should be in the matrix with their requiresNugets values preserved
        Assert.Equal(2, combinedMatrix.Include.Length);
        Assert.Contains(combinedMatrix.Include, e => e.ProjectName == "NugetsProject" && e.RequiresNugets == true);
        Assert.Contains(combinedMatrix.Include, e => e.ProjectName == "NoNugetsProject" && e.RequiresNugets == false);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
        Assert.Empty(expanded.Include);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task FailsWhenCanonicalMatrixNotFound()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDir.Path, "does-not-exist.json");
        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(nonExistentFile, outputMatrixFile: outputFile);

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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        // Should warn about the invalid OS
        Assert.Contains("invalid", result.Output.ToLowerInvariant());

        var expanded = ParseGitHubMatrix(outputFile);
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

        var outputFile = Path.Combine(_tempDir.Path, "expanded.json");

        // Act
        var result = await RunScript(canonicalMatrix, outputMatrixFile: outputFile);

        // Assert
        result.EnsureSuccessful();

        var expanded = ParseGitHubMatrix(outputFile);
        Assert.Equal(3, expanded.Include.Length);
        Assert.Contains(expanded.Include, e => e.RunsOn == "windows-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "ubuntu-latest");
        Assert.Contains(expanded.Include, e => e.RunsOn == "macos-latest");
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
