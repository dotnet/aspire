// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.CommandLine;
using Aspire.TestTools;

// Usage: dotnet tools run GenerateTestSummary --dirPathOrTrxFilePath <path> [--output <output>] [--combined]
// Generate a summary report from trx files.
// And write to $GITHUB_STEP_SUMMARY if running in GitHub Actions.

var dirPathOrTrxFilePathArgument = new Argument<string>("dirPathOrTrxFilePath");
var outputOption = new Option<string>("--output", "-o") { Description = "Output file path" };
var combinedSummaryOption = new Option<bool>("--combined", "-c") { Description = "Generate combined summary report" };
var urlOption = new Option<string>("--url", "-u") { Description = "URL for test links" };

var rootCommand = new RootCommand
{
    dirPathOrTrxFilePathArgument,
    outputOption,
    combinedSummaryOption,
    urlOption
};

rootCommand.SetAction(result =>
{
    var dirPathOrTrxFilePath = result.GetValue<string>(dirPathOrTrxFilePathArgument);
    if (string.IsNullOrEmpty(dirPathOrTrxFilePath))
    {
        Console.WriteLine("Please provide a directory path with trx files or a trx file path.");
        return;
    }

    var combinedSummary = result.GetValue<bool>(combinedSummaryOption);

    string report;
    if (combinedSummary)
    {
        report = TestSummaryGenerator.CreateCombinedTestSummaryReport(dirPathOrTrxFilePath);
    }
    else
    {
        var reportBuilder = new StringBuilder();
        if (Directory.Exists(dirPathOrTrxFilePath))
        {
            var trxFiles = Directory.EnumerateFiles(dirPathOrTrxFilePath, "*.trx", SearchOption.AllDirectories);
            foreach (var trxFile in trxFiles)
            {
                TestSummaryGenerator.CreateSingleTestSummaryReport(trxFile, reportBuilder);
            }
        }
        else
        {
            TestSummaryGenerator.CreateSingleTestSummaryReport(dirPathOrTrxFilePath, reportBuilder, result.GetValue<string>(urlOption));
        }

        report = reportBuilder.ToString();
    }

    var outputFilePath = result.GetValue<string>(outputOption);
    if (outputFilePath is not null)
    {
        File.WriteAllText(outputFilePath, report);
        Console.WriteLine($"Report written to {outputFilePath}");
    }

    if (report.Length > 0
        && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
        && Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is string summaryPath
        && !string.IsNullOrEmpty(summaryPath))
    {
        File.WriteAllText(summaryPath, report);
    }
});

return rootCommand.Parse(args).Invoke();
