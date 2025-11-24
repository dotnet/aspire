// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.TestTools;

sealed partial class TestSummaryGenerator
{
    public static string CreateCombinedTestSummaryReport(string basePath)
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

        // Collect all test run data first so we can sort by duration
        var testRunData = new List<TestRunSummary>();

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

            // collect data for each trx file
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

            var icon = total == 0 ? "⚠️"
                        : failed > 0 ? "❌"
                            : passed > 0 ? "✅"
                                : "❓";

            var duration = TrxReader.GetTestRunDurationInMinutes(testRun);

            testRunData.Add(new TestRunSummary(
                Icon: icon,
                Os: os,
                Title: GetTestTitle(filePath),
                Passed: passed,
                Failed: failed,
                Skipped: skipped,
                Total: total,
                DurationMinutes: duration
            ));
        }

        // Sort by duration descending
        testRunData.Sort((x, y) => y.DurationMinutes.CompareTo(x.DurationMinutes));

        // Build the table with sorted data
        var tableBuilder = new StringBuilder();
        tableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total | Duration (minutes) |");
        tableBuilder.AppendLine("|------|--------|--------|---------|-------|-------------------|");

        foreach (var data in testRunData)
        {
            tableBuilder.AppendLine(CultureInfo.InvariantCulture, 
                $"| {data.Icon} [{data.Os}] {data.Title} | {data.Passed} | {data.Failed} | {data.Skipped} | {data.Total} | {data.DurationMinutes:F2} |");
        }

        var overallTableBuilder = new StringBuilder();
        overallTableBuilder.AppendLine("## Overall Summary");

        overallTableBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
        overallTableBuilder.AppendLine("|--------|--------|---------|-------|");
        overallTableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {overallPassedTestCount} | {overallFailedTestCount} | {overallSkippedTestCount} | {overallTotalTestCount} |");

        overallTableBuilder.AppendLine();
        overallTableBuilder.Append(tableBuilder);

        // Add duration statistics
        overallTableBuilder.AppendLine();
        overallTableBuilder.AppendLine("## Duration Statistics");
        overallTableBuilder.Append(GenerateDurationStatistics(basePath));

        return overallTableBuilder.ToString();
    }

    public static void CreateSingleTestSummaryReport(string trxFilePath, StringBuilder reportBuilder, string? url)
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
        if (failed == 0)
        {
            Console.WriteLine($"No failed tests in {trxFilePath}");
            return;
        }

        var total = counters.Total;
        var passed = counters.Passed;
        var skipped = counters.NotExecuted;

        var title = string.IsNullOrEmpty(url)
            ? GetTestTitle(trxFilePath)
            : $"{GetTestTitle(trxFilePath)} (<a href=\"{url}\">Logs</a>)";

        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {title}");
        reportBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
        reportBuilder.AppendLine("|--------|--------|---------|-------|");
        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {passed} | {failed} | {skipped} | {total} |");

        reportBuilder.AppendLine();
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
        reportBuilder.AppendLine();
    }

    private static string GenerateDurationStatistics(string basePath)
    {
        var allDurations = new List<double>();
        var testDetails = new List<(string TestName, double DurationSeconds, string Outcome, string TestRun)>();

        var trxFiles = Directory.EnumerateFiles(basePath, "*.trx", SearchOption.AllDirectories);
        foreach (var filePath in trxFiles)
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(filePath);
                if (testRun?.Results?.UnitTestResults is null)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            var testRunName = GetTestTitle(filePath);

            foreach (var test in testRun.Results.UnitTestResults)
            {
                if (test.Duration is string durationStr && TimeSpan.TryParse(durationStr, out var duration))
                {
                    var seconds = duration.TotalSeconds;
                    allDurations.Add(seconds);
                    testDetails.Add((test.TestName ?? "Unknown", seconds, test.Outcome ?? "Unknown", testRunName));
                }
            }
        }

        if (allDurations.Count == 0)
        {
            return "No test duration data available.\n";
        }

        var statsBuilder = new StringBuilder();

        // Calculate statistics
        allDurations.Sort();
        var count = allDurations.Count;
        var sum = allDurations.Sum();
        var mean = sum / count;
        var median = allDurations[count / 2];

        // Calculate standard deviation
        var variance = allDurations.Select(d => Math.Pow(d - mean, 2)).Sum() / count;
        var stdDev = Math.Sqrt(variance);

        // Percentiles
        var p50 = allDurations[(int)(count * 0.50)];
        var p90 = allDurations[(int)(count * 0.90)];
        var p95 = allDurations[(int)(count * 0.95)];
        var p99 = allDurations[(int)(count * 0.99)];

        // Basic statistics table
        statsBuilder.AppendLine("### Overall Statistics");
        statsBuilder.AppendLine();
        statsBuilder.AppendLine("| Metric | Value |");
        statsBuilder.AppendLine("|--------|-------|");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Total Tests | {count:N0} |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Total Time | {sum / 60:F2} minutes |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Mean | {mean:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Median | {median:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Std Dev | {stdDev:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Min | {allDurations[0]:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| Max | {allDurations[^1]:F3}s |");
        statsBuilder.AppendLine();

        // Percentiles table
        statsBuilder.AppendLine("### Percentiles");
        statsBuilder.AppendLine();
        statsBuilder.AppendLine("| Percentile | Duration |");
        statsBuilder.AppendLine("|------------|----------|");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 50th (Median) | {p50:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 90th | {p90:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 95th | {p95:F3}s |");
        statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| 99th | {p99:F3}s |");
        statsBuilder.AppendLine();

        // Distribution buckets
        statsBuilder.AppendLine("### Duration Distribution");
        statsBuilder.AppendLine();
        var buckets = new (string Label, double Min, double Max, int Count)[]
        {
            ("< 1s", 0, 1, 0),
            ("1-5s", 1, 5, 0),
            ("5-10s", 5, 10, 0),
            ("10-30s", 10, 30, 0),
            ("30-60s", 30, 60, 0),
            ("> 60s", 60, double.MaxValue, 0)
        };

        var bucketCounts = new int[buckets.Length];
        foreach (var duration in allDurations)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                if (duration >= buckets[i].Min && duration < buckets[i].Max)
                {
                    bucketCounts[i]++;
                    break;
                }
            }
        }

        statsBuilder.AppendLine("| Range | Count | Percentage |");
        statsBuilder.AppendLine("|-------|-------|------------|");
        for (int i = 0; i < buckets.Length; i++)
        {
            var percentage = (bucketCounts[i] / (double)count) * 100;
            statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {buckets[i].Label} | {bucketCounts[i]:N0} | {percentage:F1}% |");
        }
        statsBuilder.AppendLine();

        // Top 10 slowest tests
        statsBuilder.AppendLine("### Top 10 Slowest Tests");
        statsBuilder.AppendLine();
        var slowestTests = testDetails.OrderByDescending(t => t.DurationSeconds).Take(10);
        statsBuilder.AppendLine("| Duration | Status | Test Name | Test Run |");
        statsBuilder.AppendLine("|----------|--------|-----------|----------|");

        foreach (var test in slowestTests)
        {
            var icon = test.Outcome == "Passed" ? "✅" : test.Outcome == "Failed" ? "❌" : "⚠️";
            statsBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {test.DurationSeconds:F2}s | {icon} {test.Outcome} | {test.TestName} | {test.TestRun} |");
        }
        statsBuilder.AppendLine();

        return statsBuilder.ToString();
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

    [GeneratedRegex(@"(?<testName>.*)_(?<tfm>net\d+\.0)_.*")]
    private static partial Regex TestNameFromTrxFileNameRegex();
}

internal sealed record TestRunSummary(string Icon, string Os, string Title, int Passed, int Failed, int Skipped, int Total, double DurationMinutes);
