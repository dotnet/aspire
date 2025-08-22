// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aspire.TestTools;

// Usage: dotnet run --project tools/PostTestSummaryToCheck -- <testResultsPath> --check-name <name> --repo <owner/repo> --sha <commit> --url <artifact-url>
// Creates a GitHub Check with test summary from TRX files.

var testResultsPathArgument = new Argument<string>("testResultsPath");
var checkNameOption = new Option<string>("--check-name") { Description = "Name for the GitHub Check" };
var repoOption = new Option<string>("--repo") { Description = "Repository in owner/repo format" };
var shaOption = new Option<string>("--sha") { Description = "Commit SHA" };
var urlOption = new Option<string>("--url") { Description = "URL for artifact logs" };
var checkSuiteIdOption = new Option<long?>("--check-suite-id") { Description = "Check suite ID to associate the check with (ensures it shows under the current workflow)" };

var rootCommand = new RootCommand
{
    testResultsPathArgument,
    checkNameOption,
    repoOption,
    shaOption,
    urlOption,
    checkSuiteIdOption
};

rootCommand.SetAction(result =>
{
    var testResultsPath = result.GetValue<string>(testResultsPathArgument);
    var checkName = result.GetValue<string>(checkNameOption);
    var repo = result.GetValue<string>(repoOption);
    var sha = result.GetValue<string>(shaOption);
    var url = result.GetValue<string>(urlOption);
    var checkSuiteId = result.GetValue<long?>(checkSuiteIdOption);

    if (string.IsNullOrEmpty(testResultsPath))
    {
        Console.WriteLine("Error: testResultsPath is required.");
        return;
    }

    if (string.IsNullOrEmpty(checkName))
    {
        Console.WriteLine("Error: --check-name is required.");
        return;
    }

    if (string.IsNullOrEmpty(repo))
    {
        Console.WriteLine("Error: --repo is required.");
        return;
    }

    if (string.IsNullOrEmpty(sha))
    {
        Console.WriteLine("Error: --sha is required.");
        return;
    }

    if (!Directory.Exists(testResultsPath))
    {
        Console.WriteLine($"Error: Test results directory not found: {testResultsPath}");
        return;
    }

    var trxFiles = Directory.GetFiles(testResultsPath, "*.trx", SearchOption.AllDirectories);
    if (trxFiles.Length == 0)
    {
        Console.WriteLine($"Warning: No TRX files found in {testResultsPath}");
        return;
    }

    // Generate test summary for all tests (successful and failed)
    var summaryBuilder = new StringBuilder();
    var hasAnyTests = false;

    foreach (var trxFile in trxFiles)
    {
        try
        {
            var testRun = TrxReader.DeserializeTrxFile(trxFile);
            if (testRun?.ResultSummary?.Counters is null)
            {
                Console.WriteLine($"Warning: Could not read test results from {trxFile}");
                continue;
            }

            hasAnyTests = true;
            var counters = testRun.ResultSummary.Counters;
            var title = GetTestTitle(Path.GetFileName(trxFile));

            if (!string.IsNullOrEmpty(url))
            {
                title = $"{title} (<a href=\"{url}\">Logs</a>)";
            }

            summaryBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {title}");
            summaryBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
            summaryBuilder.AppendLine("|--------|--------|---------|-------|");
            summaryBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {counters.Passed} | {counters.Failed} | {counters.NotExecuted} | {counters.Total} |");
            summaryBuilder.AppendLine();

            // Add detailed failure information if there are failures
            if (counters.Failed > 0 && testRun.Results?.UnitTestResults is not null)
            {
                var failedTests = testRun.Results.UnitTestResults.Where(r => r.Outcome == "Failed");
                foreach (var failedTest in failedTests)
                {
                    summaryBuilder.AppendLine("<div>");
                    summaryBuilder.AppendLine(CultureInfo.InvariantCulture, $"    <details><summary>🔴 <b>{failedTest.TestName}</b></summary>");
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine("```yml");

                    var errorMessage = failedTest.Output?.ErrorInfoString ?? "Test failed";

                    summaryBuilder.AppendLine(errorMessage);

                    summaryBuilder.AppendLine("```");
                    summaryBuilder.AppendLine();
                    summaryBuilder.AppendLine("</div>");
                }
            }
            else if (counters.Failed == 0)
            {
                summaryBuilder.AppendLine("✅ All tests passed!");
            }

            summaryBuilder.AppendLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not process TRX file {trxFile}: {ex.Message}");
        }
    }

    if (!hasAnyTests)
    {
        Console.WriteLine("No test results found to process");
        return;
    }

    var summaryContent = summaryBuilder.ToString();

    // Determine if there are test failures
    var hasFailures = false;
    foreach (var trxFile in trxFiles)
    {
        try
        {
            var testRun = TrxReader.DeserializeTrxFile(trxFile);
            var counters = testRun?.ResultSummary?.Counters;
            if (counters is not null && (counters.Failed > 0 || counters.Error > 0))
            {
                hasFailures = true;
                break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not read TRX file {trxFile}: {ex.Message}");
            hasFailures = true; // Assume failure if we can't read the file
            break;
        }
    }

    // If there are no failures, do not create a Check at all (silent success)
    if (!hasFailures)
    {
        Console.WriteLine("No test failures detected. Skipping GitHub Check creation.");
        return;
    }

    var conclusion = "failure"; // Only posting checks on failure per policy
    var fullCheckName = $"CHECKTEST {checkName}";

    Console.WriteLine($"Creating GitHub Check: {fullCheckName}");
    Console.WriteLine($"Repository: {repo}");
    Console.WriteLine($"Commit SHA: {sha}");
    Console.WriteLine($"Conclusion: {conclusion}");

    // Write summary to temp file for gh api
    var summaryFile = Path.GetTempFileName();
    try
    {
        File.WriteAllText(summaryFile, summaryContent, Encoding.UTF8);

        // Create the check using gh api
    var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "gh",
        Arguments = $"api --method POST " +
                          $"-H \"Accept: application/vnd.github+json\" " +
                          $"-H \"X-GitHub-Api-Version: 2022-11-28\" " +
                          $"--verbose " +
                          $"/repos/{repo}/check-runs " +
                          $"-f \"name={fullCheckName}\" " +
                          $"-f \"head_sha={sha}\" " +
                          $"-f \"status=completed\" " +
                          $"-f \"conclusion={conclusion}\" " +
                          $"-f \"output[title]={fullCheckName} Test Results\" " +
              $"-F \"output[summary]=@{summaryFile}\"" +
              (checkSuiteId is long id ? $" -f \"check_suite_id={id}\"" : string.Empty),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        var output = outputTask.Result;
        var error = errorTask.Result;

        if (process.ExitCode == 0)
        {
            Console.WriteLine("✅ GitHub Check created successfully");
        }
        else
        {
            Console.WriteLine("❌ Failed to create GitHub Check");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine($"Error: {error}");
            Environment.Exit(1);
        }
    }
    finally
    {
        File.Delete(summaryFile);
    }
});

return rootCommand.Parse(args).Invoke();

static string GetTestTitle(string fileName)
{
    // Extract test name from filename, e.g., "Seq.trx" -> "Seq"
    var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

    // Remove common suffixes to get clean test name
    var cleanName = nameWithoutExtension
        .Replace(".Tests", "")
        .Replace("Aspire.", "")
        .Replace("_net8.0_x64", "")
        .Replace("_net10.0_x64", "");

    return cleanName;
}
