// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Tests for eng/scripts/filter-test-matrix-by-scope.ps1
/// </summary>
public class FilterTestMatrixByScopeTests : IDisposable
{
    private readonly TestTempDirectory _tempDir = new();
    private readonly string _scriptPath;
    private readonly ITestOutputHelper _output;

    public FilterTestMatrixByScopeTests(ITestOutputHelper output)
    {
        _output = output;
        _scriptPath = Path.Combine(FindRepoRoot(), "eng", "scripts", "filter-test-matrix-by-scope.ps1");
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RunAll_PassesThrough()
    {
        var matrix = CreateMatrix(
            CreateEntry("Proj-linux", "tests/ProjA/ProjA.csproj"),
            CreateEntry("Proj-win", "tests/ProjB/ProjB.csproj"));

        var result = await RunFilter(matrix, "[]", runAll: true);

        result.EnsureSuccessful("RunAll should pass through");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        Assert.Equal(2, filtered.Length);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task EmptyAffectedList_PassesThrough()
    {
        var matrix = CreateMatrix(
            CreateEntry("Proj-linux", "tests/ProjA/ProjA.csproj"));

        var result = await RunFilter(matrix, "[]", runAll: false);

        result.EnsureSuccessful("Empty affected list should pass through");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        Assert.Single(filtered);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task FiltersToAffectedProjects()
    {
        var matrix = CreateMatrix(
            CreateEntry("ProjA-linux", "tests/ProjA/ProjA.csproj"),
            CreateEntry("ProjB-linux", "tests/ProjB/ProjB.csproj"),
            CreateEntry("ProjC-linux", "tests/ProjC/ProjC.csproj"));

        var affected = JsonSerializer.Serialize(new[] { "tests/ProjA/ProjA.csproj", "tests/ProjC/ProjC.csproj" });

        var result = await RunFilter(matrix, affected, runAll: false);

        result.EnsureSuccessful("Should filter to affected projects");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        Assert.Equal(2, filtered.Length);
        Assert.Contains(filtered, e => e.GetProperty("shortname").GetString() == "ProjA-linux");
        Assert.Contains(filtered, e => e.GetProperty("shortname").GetString() == "ProjC-linux");
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task NoMatchingProjects_ReturnsEmptyMatrix()
    {
        var matrix = CreateMatrix(
            CreateEntry("ProjA-linux", "tests/ProjA/ProjA.csproj"));

        var affected = JsonSerializer.Serialize(new[] { "tests/Unrelated/Unrelated.csproj" });

        var result = await RunFilter(matrix, affected, runAll: false);

        result.EnsureSuccessful("Should return empty matrix");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        Assert.Empty(filtered);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task AuditOnly_LogsButDoesNotFilter()
    {
        var matrix = CreateMatrix(
            CreateEntry("ProjA-linux", "tests/ProjA/ProjA.csproj"),
            CreateEntry("ProjB-linux", "tests/ProjB/ProjB.csproj"));

        var affected = JsonSerializer.Serialize(new[] { "tests/ProjA/ProjA.csproj" });

        var result = await RunFilter(matrix, affected, runAll: false, auditOnly: true);

        result.EnsureSuccessful("AuditOnly should succeed");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        // AuditOnly returns full matrix unchanged
        Assert.Equal(2, filtered.Length);
        // But logs should mention what would be filtered
        Assert.Contains("AUDIT", result.Output);
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PathNormalization()
    {
        var matrix = CreateMatrix(
            CreateEntry("ProjA-linux", "tests/ProjA/ProjA.csproj"));

        // Affected list uses backslashes
        var affected = JsonSerializer.Serialize(new[] { @"tests\ProjA\ProjA.csproj" });

        var result = await RunFilter(matrix, affected, runAll: false);

        result.EnsureSuccessful("Should handle backslash paths");
        var filtered = ParseOutputMatrix(result.Output, "test_matrix");
        Assert.Single(filtered);
    }

    private async Task<CommandResult> RunFilter(
        string matrixJson,
        string affectedProjects,
        bool runAll,
        bool auditOnly = false)
    {
        // Write a small wrapper script that calls filter-test-matrix-by-scope.ps1
        // with hashtable parameter (can't pass hashtable directly from CLI)
        var wrapperScript = Path.Combine(_tempDir.Path, "run-filter.ps1");
        var runAllSwitch = runAll ? "-RunAll" : "";
        var auditSwitch = auditOnly ? "-AuditOnly" : "";
        File.WriteAllText(wrapperScript, $$"""
            $matrices = @{
                'test_matrix' = '{{matrixJson.Replace("'", "''")}}'
            }
            & '{{_scriptPath}}' `
                -Matrices $matrices `
                -AffectedProjects '{{affectedProjects.Replace("'", "''")}}' `
                {{runAllSwitch}} {{auditSwitch}}
            """);

        using var cmd = new PowerShellCommand(wrapperScript, _output)
            .WithTimeout(TimeSpan.FromMinutes(1));

        return await cmd.ExecuteAsync();
    }

    private static string CreateMatrix(params object[] entries)
    {
        var matrix = new { include = entries };
        return JsonSerializer.Serialize(matrix);
    }

    private static object CreateEntry(string shortname, string testProjectPath)
    {
        return new
        {
            shortname,
            testProjectPath,
            testSessionTimeout = "20m",
            testHangTimeout = "10m"
        };
    }

    private static JsonElement[] ParseOutputMatrix(string output, string matrixName)
    {
        // Find the line with the matrix name followed by JSON content
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var prefix = $"{matrixName}: ";
            if (trimmed.StartsWith(prefix))
            {
                var rest = trimmed[prefix.Length..].Trim();
                // Only try to parse if it looks like JSON (starts with {)
                if (rest.StartsWith('{'))
                {
                    var parsed = JsonDocument.Parse(rest);
                    if (parsed.RootElement.TryGetProperty("include", out var include))
                    {
                        return include.EnumerateArray().ToArray();
                    }
                    return [];
                }
            }
        }
        return [];
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
