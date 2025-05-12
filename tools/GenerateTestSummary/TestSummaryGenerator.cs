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
        if (!Directory.Exists(basePath))
        {
            throw new DirectoryNotFoundException($"The directory '{basePath}' does not exist.");
        }

        var trxFiles = System.IO.Directory.EnumerateFiles(basePath, "*.trx", System.IO.SearchOption.AllDirectories);

        int overallTotalTestCount = 0;
        int overallPassedTestCount = 0;
        int overallFailedTestCount = 0;
        int overallSkippedTestCount = 0;

        // Update to use markdown tables instead of HTML
        var tableBuilder = new StringBuilder();
        tableBuilder.AppendLine("| Name | Passed | Failed | Skipped | Total |");
        tableBuilder.AppendLine("|------|--------|--------|---------|-------|");

        foreach (var file in trxFiles.OrderBy(f => Path.GetFileName(f)))
        {
            TestRun? testRun;
            try
            {
                testRun = TrxReader.DeserializeTrxFile(file);
                if (testRun == null || testRun.ResultSummary?.Counters == null)
                {
                    Console.WriteLine($"Failed to deserialize or find results in file: {file}, tr: {testRun}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize file: {file}, exception: {ex}");
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
            var os = file.Contains("windows-")
                        ? "win"
                        : file.Contains("ubuntu-")
                            ? "lin"
                            : file.Contains("macos-")
                                ? "mac"
                                : "os?";

            tableBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {(failed > 0 ? "❌" : "✅")} [{os}] {GetTestTitle(file)} | {passed} | {failed} | {skipped} | {total} |");
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

    public static void CreateSingleTestSummaryReport(string trxFilePath, StringBuilder reportBuilder, string? url = null)
    {
        if (!File.Exists(trxFilePath))
        {
            throw new FileNotFoundException($"The file '{trxFilePath}' does not exist.");
        }

        TestRun? testRun;
        try
        {
            testRun = TrxReader.DeserializeTrxFile(trxFilePath);
            if (testRun == null || testRun.ResultSummary?.Counters == null)
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
            return;
        }

        var total = counters.Total;
        var passed = counters.Passed;
        var skipped = counters.NotExecuted;

        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {GetTestTitle(trxFilePath)}");
        reportBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
        reportBuilder.AppendLine("|--------|--------|---------|-------|");
        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {passed} | {failed} | {skipped} | {total} |");

        reportBuilder.AppendLine();
        if (testRun.Results?.UnitTestResults is null)
        {
            return;
        }

        var failedTests = testRun.Results.UnitTestResults.Where(r => r.Outcome == "Failed");
        if (failedTests.Any())
        {
            foreach (var test in failedTests)
            {
                var title = string.IsNullOrEmpty(url)
                                ? $"🔴 <b>{test.TestName}</b>"
                                : $"🔴 <a href=\"{url}\">{test.TestName}</a>";

                reportBuilder.AppendLine("<div>");
                reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"""
                    <details><summary>{title}</summary>
                """);

                var errorMsgBuilder = new StringBuilder();
                errorMsgBuilder.AppendLine(test.Output?.ErrorInfo?.InnerText ?? string.Empty);
                errorMsgBuilder.AppendLine(test.Output?.StdOut ?? string.Empty);

                // Truncate long error messages for readability
                var errorMsgTruncated = TruncateTheStart(errorMsgBuilder.ToString(), 50_000);

                reportBuilder.AppendLine();
                reportBuilder.AppendLine("```yml");
                reportBuilder.AppendLine(errorMsgTruncated);
                reportBuilder.AppendLine("```");
                reportBuilder.AppendLine();
                reportBuilder.AppendLine("</div>");
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

    [GeneratedRegex(@"(?<testName>.*)_(?<tfm>net\d+\.0)_.*")]
    private static partial Regex TestNameFromTrxFileNameRegex();

    private static string? TruncateTheStart(string? s, int maxLength)
        => s is null || s.Length <= maxLength
            ? s
            : "... (truncated) " + s[^maxLength..];
}
