// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.TestTools;

sealed partial class TestSummaryGenerator
{
    public static string CreateCombinedTestSummaryReport(string basePath, bool errorOnZeroTests = false)
    {
        var resolved = Path.GetFullPath(basePath);
        if (!Directory.Exists(resolved))
        {
            throw new DirectoryNotFoundException($"The directory '{resolved}' does not exist.");
        }

        int overallTotalTestCount = 0;
        int overallPassedTestCount = 0;
        int overallFailedTestCount = 0;
        int overallSkippedTestCount = 0;

        // Update to use markdown tables instead of HTML
        var tableBuilder = new StringBuilder();
        tableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total |");
        tableBuilder.AppendLine("|------|--------|--------|---------|-------|");

        var trxFiles = Directory.EnumerateFiles(basePath, "*.trx", SearchOption.AllDirectories);
        foreach (var filePath in trxFiles.OrderBy(f => Path.GetFileName(f)))
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(filePath);
                if (testRun?.ResultSummary?.Counters is null)
                {
                    Console.WriteLine($"Failed to deserialize or find results in file: {filePath}, tr: {testRun}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize file: {filePath}, exception: {ex}");
                continue;
            }

            // emit row for each trx file
            var counters = testRun.ResultSummary.Counters;
            int total = counters.Total;
            int passed = counters.Passed;
            int failed = counters.Failed;
            int skipped = counters.NotExecuted;

            overallTotalTestCount += total;
            overallPassedTestCount += passed;
            overallFailedTestCount += failed;
            overallSkippedTestCount += skipped;

            // Determine the OS from the path, assuming the path contains
            // os runner name like `windows-latest`, `ubuntu-latest`, or `macos-latest`
            var os = filePath.Contains("windows-")
                        ? "win"
                        : filePath.Contains("ubuntu-")
                            ? "lin"
                            : filePath.Contains("macos-")
                                ? "mac"
                                : throw new InvalidOperationException($"Could not determine OS from file path: {filePath}");

            tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {GetStatusSymbol(failed, total, errorOnZeroTests)} [{os}] {GetTestTitle(filePath)} | {passed} | {failed} | {skipped} | {total} |");
        }

        var overallTableBuilder = new StringBuilder();
        overallTableBuilder.AppendLine("## Overall Summary");

        overallTableBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
        overallTableBuilder.AppendLine("|--------|--------|---------|-------|");
        overallTableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {overallPassedTestCount} | {overallFailedTestCount} | {overallSkippedTestCount} | {overallTotalTestCount} |");

        overallTableBuilder.AppendLine();
        overallTableBuilder.Append(tableBuilder);

        return overallTableBuilder.ToString();
    }

    public static void CreateSingleTestSummaryReport(string trxFilePath, StringBuilder reportBuilder, string? url, bool errorOnZeroTests = false)
    {
        if (!File.Exists(trxFilePath))
        {
            throw new FileNotFoundException($"The file '{trxFilePath}' does not exist.");
        }

        TestRun? testRun;
        try
        {
            testRun = TrxReader.DeserializeTrxFile(trxFilePath);
            if (testRun?.ResultSummary?.Counters is null)
            {
                throw new InvalidOperationException($"Failed to deserialize or find results in file: {trxFilePath}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to process file: {trxFilePath}", ex);
        }

        var counters = testRun.ResultSummary.Counters;
        var failed = counters.Failed;
        var total = counters.Total;
        var passed = counters.Passed;
        var skipped = counters.NotExecuted;

        // Restore original behavior: no report when tests ran and all passed
        if (total > 0 && failed == 0)
        {
            Console.WriteLine($"No failed tests in {trxFilePath}");
            return;
        }

        // Generate report for zero tests or when there are failures
        var title = string.IsNullOrEmpty(url)
            ? GetTestTitle(trxFilePath)
            : $"{GetTestTitle(trxFilePath)} (<a href=\"{url}\">Logs</a>)";

        var statusSymbol = GetStatusSymbol(failed, total, errorOnZeroTests);
        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {statusSymbol} {title}");
        
        // Special handling for zero tests - show message instead of table
        if (total == 0)
        {
            reportBuilder.AppendLine("Zero tests were run. Consider disabling this in the test project using $(RunOnGithubActionsLinux) like properties");
        }
        else
        {
            reportBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
            reportBuilder.AppendLine("|--------|--------|---------|-------|");
            reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {passed} | {failed} | {skipped} | {total} |");
        }

        reportBuilder.AppendLine();

        // Only show detailed failed test information if there are failures
        if (failed > 0)
        {
            if (testRun.Results?.UnitTestResults is null)
            {
                Console.WriteLine($"Could not find any UnitTestResult entries in {trxFilePath}");
                return;
            }

            var failedTests = testRun.Results.UnitTestResults.Where(r => r.Outcome == "Failed");
            if (failedTests.Any())
            {
                foreach (var test in failedTests)
                {
                    reportBuilder.AppendLine("<div>");
                    reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"""
                        <details><summary>🔴 <b>{test.TestName}</b></summary>

                    """);

                    reportBuilder.AppendLine();
                    reportBuilder.AppendLine("```yml");

                    reportBuilder.AppendLine(test.Output?.ErrorInfo?.InnerText);
                    if (test.Output?.StdOut is not null)
                    {
                        const int halfLength = 25_000;
                        var stdOutSpan = test.Output.StdOut.AsSpan();

                        reportBuilder.AppendLine();
                        reportBuilder.AppendLine("### StdOut");

                        var startSpan = stdOutSpan[..Math.Min(halfLength, stdOutSpan.Length)];
                        reportBuilder.AppendLine(startSpan.ToString());

                        if (stdOutSpan.Length > halfLength)
                        {
                            reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"{Environment.NewLine}... (snip) ...{Environment.NewLine}");
                            var endSpan = stdOutSpan[^halfLength..];
                            // `endSpan` might not begin at the true beginning of the original line
                            reportBuilder.Append("... ");
                            reportBuilder.Append(endSpan);
                        }
                    }

                    reportBuilder.AppendLine("```");
                    reportBuilder.AppendLine();
                    reportBuilder.AppendLine("</div>");
                }
            }
        }
        reportBuilder.AppendLine();
    }

    public static string GetTestTitle(string trxFileName)
    {
        var filename = Path.GetFileNameWithoutExtension(trxFileName);
        var match = TestNameFromTrxFileNameRegex().Match(filename);
        if (match.Success)
        {
            return $"{match.Groups["testName"].Value} ({match.Groups["tfm"].Value})";
        }

        return filename;
    }

    private static string GetStatusSymbol(int failed, int total, bool errorOnZeroTests)
    {
        if (failed > 0)
        {
            return "❌";
        }
        
        if (total == 0)
        {
            return errorOnZeroTests ? "❌" : "⚠️";
        }
        
        return "✅";
    }

    [GeneratedRegex(@"(?<testName>.*)_(?<tfm>net\d+\.0)_.*")]
    private static partial Regex TestNameFromTrxFileNameRegex();
}
