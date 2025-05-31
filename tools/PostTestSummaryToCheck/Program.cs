// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Aspire.TestTools;

// Usage: dotnet run --project tools/PostTestSummaryToCheck -- <testResultsPath> --check-name <name> --repo <owner/repo> --sha <commit> --url <artifact-url>
// Creates a GitHub Check with test summary from TRX files.

var testResultsPathArgument = new Argument<string>("testResultsPath");
var checkNameOption = new Option<string>("--check-name") { Description = "Name for the GitHub Check" };
var repoOption = new Option<string>("--repo") { Description = "Repository in owner/repo format" };
var shaOption = new Option<string>("--sha") { Description = "Commit SHA" };
var urlOption = new Option<string>("--url") { Description = "URL for artifact logs" };

var rootCommand = new RootCommand
{
    testResultsPathArgument,
    checkNameOption,
    repoOption,
    shaOption,
    urlOption
};

rootCommand.SetAction(result =>
{
    var testResultsPath = result.GetValue<string>(testResultsPathArgument);
    var checkName = result.GetValue<string>(checkNameOption);
    var repo = result.GetValue<string>(repoOption);
    var sha = result.GetValue<string>(shaOption);
    var url = result.GetValue<string>(urlOption);

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

    // Generate test summary using existing tool
    var summaryBuilder = new StringBuilder();
    foreach (var trxFile in trxFiles)
    {
        TestSummaryGenerator.CreateSingleTestSummaryReport(trxFile, summaryBuilder, url);
    }

    var summaryContent = summaryBuilder.ToString();
    if (string.IsNullOrWhiteSpace(summaryContent))
    {
        Console.WriteLine("No test summary generated");
        return;
    }

    // Determine if there are test failures
    var hasFailures = false;
    foreach (var trxFile in trxFiles)
    {
        try
        {
            var testRun = TrxReader.DeserializeTrxFile(trxFile);
            if (testRun?.ResultSummary?.Counters?.Failed > 0)
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

    var conclusion = hasFailures ? "failure" : "success";
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
                          $"-F \"output[summary]=@{summaryFile}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

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