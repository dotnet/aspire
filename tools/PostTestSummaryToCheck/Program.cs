// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Aspire.TestTools;

// Usage: PostTestSummaryToCheck --test-results-path <path> --github-token <token> --repository <repo> --commit-sha <sha> --check-name <name>

var testResultsPathOption = new Option<string>("--test-results-path") { Description = "Path to test results directory or TRX file" };
var githubTokenOption = new Option<string>("--github-token") { Description = "GitHub token for API access" };
var repositoryOption = new Option<string>("--repository") { Description = "Repository in format 'owner/repo'" };
var commitShaOption = new Option<string>("--commit-sha") { Description = "Commit SHA" };
var checkNameOption = new Option<string>("--check-name") { Description = "Name of the GitHub Check" };
var urlOption = new Option<string>("--url") { Description = "URL for test links" };

var rootCommand = new RootCommand("Post test summary to GitHub Check")
{
    testResultsPathOption,
    githubTokenOption,
    repositoryOption,
    commitShaOption,
    checkNameOption,
    urlOption
};

rootCommand.SetAction(async result =>
{
    var testResultsPath = result.GetValue<string>(testResultsPathOption);
    var githubToken = result.GetValue<string>(githubTokenOption);
    var repository = result.GetValue<string>(repositoryOption);
    var commitSha = result.GetValue<string>(commitShaOption);
    var checkName = result.GetValue<string>(checkNameOption);
    var url = result.GetValue<string>(urlOption);

    if (string.IsNullOrEmpty(testResultsPath) || string.IsNullOrEmpty(githubToken) || 
        string.IsNullOrEmpty(repository) || string.IsNullOrEmpty(commitSha) || 
        string.IsNullOrEmpty(checkName))
    {
        Console.WriteLine("Error: Required parameters are missing.");
        Environment.Exit(1);
        return;
    }

    try
    {
        await PostTestSummaryToGitHubCheck(testResultsPath, githubToken, repository, commitSha, checkName, url).ConfigureAwait(false);
        Console.WriteLine("Test summary posted to GitHub Check successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error posting test summary to GitHub Check: {ex.Message}");
        Environment.Exit(1);
    }
});

return rootCommand.Parse(args).Invoke();

static async Task PostTestSummaryToGitHubCheck(string testResultsPath, string githubToken, string repository, string commitSha, string checkName, string? url)
{
    // Generate test summary
    var summary = GenerateTestSummary(testResultsPath, url);
    
    if (string.IsNullOrEmpty(summary))
    {
        Console.WriteLine("No test results found, skipping GitHub Check creation.");
        return;
    }

    // Determine check conclusion based on test results
    var conclusion = DetermineCheckConclusion(testResultsPath);
    
    // Create GitHub Check
    await CreateGitHubCheck(githubToken, repository, commitSha, checkName, conclusion, summary).ConfigureAwait(false);
}

static string GenerateTestSummary(string testResultsPath, string? url)
{
    var reportBuilder = new StringBuilder();
    
    if (Directory.Exists(testResultsPath))
    {
        var trxFiles = Directory.EnumerateFiles(testResultsPath, "*.trx", SearchOption.AllDirectories).ToArray();
        if (trxFiles.Length == 0)
        {
            return string.Empty;
        }

        // Generate individual test summaries for each TRX file
        foreach (var trxFile in trxFiles)
        {
            TestSummaryGenerator.CreateSingleTestSummaryReport(trxFile, reportBuilder, url);
            
            // If no failed tests were shown, create a basic summary
            if (reportBuilder.Length == 0)
            {
                try
                {
                    var testRun = TrxReader.DeserializeTrxFile(trxFile);
                    if (testRun?.ResultSummary?.Counters is not null)
                    {
                        var counters = testRun.ResultSummary.Counters;
                        var passed = counters.Passed;
                        var failed = counters.Failed;
                        var skipped = counters.NotExecuted;
                        var total = counters.Total;

                        var title = string.IsNullOrEmpty(url)
                            ? TestSummaryGenerator.GetTestTitle(trxFile)
                            : $"{TestSummaryGenerator.GetTestTitle(trxFile)} (<a href=\"{url}\">Logs</a>)";

                        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {title}");
                        reportBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
                        reportBuilder.AppendLine("|--------|--------|---------|-------|");
                        reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {passed} | {failed} | {skipped} | {total} |");
                        reportBuilder.AppendLine();
                        
                        if (failed == 0)
                        {
                            reportBuilder.AppendLine("✅ All tests passed!");
                            reportBuilder.AppendLine();
                        }
                    }
                }
                catch
                {
                    // If we can't read the file, just show the filename
                    reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {Path.GetFileNameWithoutExtension(trxFile)}");
                    reportBuilder.AppendLine("Unable to read test results.");
                    reportBuilder.AppendLine();
                }
            }
        }
    }
    else if (File.Exists(testResultsPath) && testResultsPath.EndsWith(".trx"))
    {
        TestSummaryGenerator.CreateSingleTestSummaryReport(testResultsPath, reportBuilder, url);
        
        // If no failed tests were shown, create a basic summary
        if (reportBuilder.Length == 0)
        {
            try
            {
                var testRun = TrxReader.DeserializeTrxFile(testResultsPath);
                if (testRun?.ResultSummary?.Counters is not null)
                {
                    var counters = testRun.ResultSummary.Counters;
                    var passed = counters.Passed;
                    var failed = counters.Failed;
                    var skipped = counters.NotExecuted;
                    var total = counters.Total;

                    var title = string.IsNullOrEmpty(url)
                        ? TestSummaryGenerator.GetTestTitle(testResultsPath)
                        : $"{TestSummaryGenerator.GetTestTitle(testResultsPath)} (<a href=\"{url}\">Logs</a>)";

                    reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {title}");
                    reportBuilder.AppendLine("| Passed | Failed | Skipped | Total |");
                    reportBuilder.AppendLine("|--------|--------|---------|-------|");
                    reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"| {passed} | {failed} | {skipped} | {total} |");
                    reportBuilder.AppendLine();
                    
                    if (failed == 0)
                    {
                        reportBuilder.AppendLine("✅ All tests passed!");
                        reportBuilder.AppendLine();
                    }
                }
            }
            catch
            {
                // If we can't read the file, just show the filename
                reportBuilder.AppendLine(CultureInfo.InvariantCulture, $"### {Path.GetFileNameWithoutExtension(testResultsPath)}");
                reportBuilder.AppendLine("Unable to read test results.");
                reportBuilder.AppendLine();
            }
        }
    }
    else
    {
        return string.Empty;
    }

    return reportBuilder.ToString();
}

static string DetermineCheckConclusion(string testResultsPath)
{
    var trxFiles = Directory.Exists(testResultsPath) 
        ? Directory.EnumerateFiles(testResultsPath, "*.trx", SearchOption.AllDirectories)
        : File.Exists(testResultsPath) && testResultsPath.EndsWith(".trx") 
            ? [testResultsPath] 
            : [];

    var hasFailures = false;
    foreach (var trxFile in trxFiles)
    {
        try
        {
            var testRun = TrxReader.DeserializeTrxFile(trxFile);
            if (testRun?.ResultSummary?.Counters is not null)
            {
                var failed = testRun.ResultSummary.Counters.Failed;
                if (failed > 0)
                {
                    hasFailures = true;
                    break;
                }
            }
        }
        catch
        {
            // If we can't read the file, assume there might be failures
            hasFailures = true;
            break;
        }
    }

    return hasFailures ? "failure" : "success";
}

static async Task CreateGitHubCheck(string githubToken, string repository, string commitSha, string checkName, string conclusion, string summary)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    httpClient.DefaultRequestHeaders.Add("User-Agent", "Aspire-TestSummary-Check");

    var checkRun = new
    {
        name = checkName,
        head_sha = commitSha,
        status = "completed",
        conclusion = conclusion,
        output = new
        {
            title = $"{checkName} Test Results",
            summary = summary
        }
    };

    var json = JsonSerializer.Serialize(checkRun, new JsonSerializerOptions { WriteIndented = true });
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var url = $"https://api.github.com/repos/{repository}/check-runs";
    
    Console.WriteLine($"Creating GitHub Check: {checkName}");
    Console.WriteLine($"Repository: {repository}");
    Console.WriteLine($"Commit SHA: {commitSha}");
    Console.WriteLine($"Conclusion: {conclusion}");
    
    var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
    
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new InvalidOperationException($"Failed to create GitHub Check. Status: {response.StatusCode}, Content: {errorContent}");
    }
}