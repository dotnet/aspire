// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

/// <summary>
/// Tests for eng/scripts/find-test-scope-audit-misses.ps1.
/// </summary>
public class FindTestScopeAuditMissesTests : IDisposable
{
    private readonly TestTempDirectory _tempDir = new();
    private readonly string _scriptPath;
    private readonly ITestOutputHelper _output;

    public FindTestScopeAuditMissesTests(ITestOutputHelper output)
    {
        _output = output;
        _scriptPath = Path.Combine(FindRepoRoot(), "eng", "scripts", "find-test-scope-audit-misses.ps1");
    }

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task FailedProjectOutsideWouldRun_ReportsAuditMiss()
    {
        var selectorAuditPath = Path.Combine(_tempDir.Path, "selector-audit.json");
        var matrixAuditPath = Path.Combine(_tempDir.Path, "matrix-audit.json");
        var testResultsPath = Path.Combine(_tempDir.Path, "testresults");
        var reportPath = Path.Combine(_tempDir.Path, "audit-miss-report.json");

        Directory.CreateDirectory(testResultsPath);
        await File.WriteAllTextAsync(selectorAuditPath, """
            {
              "runAllTests": false,
              "reason": "selective"
            }
            """);
        await File.WriteAllTextAsync(matrixAuditPath, """
            {
              "runAll": false,
              "auditOnly": true,
              "skipFiltering": false,
              "wouldRunProjects": [
                "tests/ProjA/ProjA.csproj"
              ],
              "wouldRunEntries": [
                {
                  "matrixName": "tests_matrix_no_nugets",
                  "shortname": "ProjA",
                  "testProjectPath": "tests/ProjA/ProjA.csproj"
                },
                {
                  "matrixName": "tests_matrix_no_nugets",
                  "shortname": "ProjB",
                  "testProjectPath": "tests/ProjB/ProjB.csproj"
                }
              ],
              "templateGate": {
                "projectPath": "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj",
                "wouldRun": false
              }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(testResultsPath, "ProjB.trx"), CreateTrx(failedCount: 2));

        var result = await RunScript(selectorAuditPath, matrixAuditPath, testResultsPath, reportPath);

        result.EnsureSuccessful("Audit miss comparison should succeed");

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        var root = report.RootElement;

        Assert.True(root.GetProperty("hasAuditMiss").GetBoolean());
        Assert.Equal("ok", root.GetProperty("status").GetString());
        var misses = root.GetProperty("auditMisses").EnumerateArray().ToArray();
        Assert.Single(misses);
        Assert.Equal("tests/ProjB/ProjB.csproj", misses[0].GetProperty("testProjectPath").GetString());
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task RunAll_DoesNotReportAuditMiss()
    {
        var selectorAuditPath = Path.Combine(_tempDir.Path, "selector-audit.json");
        var matrixAuditPath = Path.Combine(_tempDir.Path, "matrix-audit.json");
        var testResultsPath = Path.Combine(_tempDir.Path, "testresults");
        var reportPath = Path.Combine(_tempDir.Path, "audit-miss-report.json");

        Directory.CreateDirectory(testResultsPath);
        await File.WriteAllTextAsync(selectorAuditPath, """
            {
              "runAllTests": true,
              "reason": "critical_path"
            }
            """);
        await File.WriteAllTextAsync(matrixAuditPath, """
            {
              "runAll": true,
              "auditOnly": true,
              "skipFiltering": true,
              "wouldRunProjects": [
                "tests/ProjA/ProjA.csproj"
              ],
              "wouldRunEntries": [
                {
                  "matrixName": "tests_matrix_no_nugets",
                  "shortname": "ProjA",
                  "testProjectPath": "tests/ProjA/ProjA.csproj"
                },
                {
                  "matrixName": "tests_matrix_no_nugets",
                  "shortname": "ProjB",
                  "testProjectPath": "tests/ProjB/ProjB.csproj"
                }
              ],
              "templateGate": {
                "projectPath": "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj",
                "wouldRun": true
              }
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(testResultsPath, "ProjB.trx"), CreateTrx(failedCount: 1));

        var result = await RunScript(selectorAuditPath, matrixAuditPath, testResultsPath, reportPath);

        result.EnsureSuccessful("RunAll audit comparison should succeed");

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        var root = report.RootElement;

        Assert.False(root.GetProperty("hasAuditMiss").GetBoolean());
        Assert.Equal("critical_path", root.GetProperty("selectorReason").GetString());
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task NoFailedTrxFiles_ReportsNoFailedTestsStatus()
    {
        var selectorAuditPath = Path.Combine(_tempDir.Path, "selector-audit.json");
        var matrixAuditPath = Path.Combine(_tempDir.Path, "matrix-audit.json");
        var testResultsPath = Path.Combine(_tempDir.Path, "testresults");
        var reportPath = Path.Combine(_tempDir.Path, "audit-miss-report.json");

        Directory.CreateDirectory(testResultsPath);
        await File.WriteAllTextAsync(selectorAuditPath, """
            {
              "runAllTests": false,
              "reason": "selective"
            }
            """);
        await File.WriteAllTextAsync(matrixAuditPath, """
            {
              "runAll": false,
              "auditOnly": true,
              "skipFiltering": false,
              "wouldRunProjects": [
                "tests/ProjA/ProjA.csproj"
              ],
              "wouldRunEntries": [
                {
                  "matrixName": "tests_matrix_no_nugets",
                  "shortname": "ProjA",
                  "testProjectPath": "tests/ProjA/ProjA.csproj"
                }
              ]
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(testResultsPath, "ProjA.trx"), CreateTrx(failedCount: 0, passedCount: 1));

        var result = await RunScript(selectorAuditPath, matrixAuditPath, testResultsPath, reportPath);

        result.EnsureSuccessful("Audit miss comparison should succeed when there are no failed TRX files");

        using var report = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
        var root = report.RootElement;

        Assert.Equal("no_failed_tests", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("hasAuditMiss").GetBoolean());
        Assert.Empty(root.GetProperty("failedProjects").EnumerateArray());
    }

    private async Task<CommandResult> RunScript(string selectorAuditPath, string matrixAuditPath, string testResultsPath, string reportPath)
    {
        var wrapperScript = Path.Combine(_tempDir.Path, "run-find-test-scope-audit-misses.ps1");
        File.WriteAllText(wrapperScript, $$"""
            & '{{_scriptPath}}' `
                -SelectorAuditPath '{{selectorAuditPath.Replace("'", "''")}}' `
                -MatrixAuditPath '{{matrixAuditPath.Replace("'", "''")}}' `
                -TestResultsRoot '{{testResultsPath.Replace("'", "''")}}' `
                -OutputPath '{{reportPath.Replace("'", "''")}}'
            """);

        using var cmd = new PowerShellCommand(wrapperScript, _output)
            .WithTimeout(TimeSpan.FromMinutes(1));

        return await cmd.ExecuteAsync();
    }

    private static string CreateTrx(int failedCount, int passedCount = 0)
    {
        var totalCount = failedCount + passedCount;

        return $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <TestRun>
              <ResultSummary>
                <Counters total="{{totalCount}}" executed="{{totalCount}}" passed="{{passedCount}}" failed="{{failedCount}}" />
              </ResultSummary>
            </TestRun>
            """;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Aspire.slnx")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException("Could not find repository root (Aspire.slnx)");
    }
}
