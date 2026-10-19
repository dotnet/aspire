// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.TestUtilities;
using Xunit;

namespace Infrastructure.Tests;

public sealed class CiShadowTests(ITestOutputHelper output) : IDisposable
{
    private const string SourceSha = "1111111111111111111111111111111111111111";
    private static readonly string[] s_classes = ["StableConnectionStringBuilderTests", "ResourceBuilderLifetimeTests"];
    private readonly TemporaryWorkspace _workspace = TemporaryWorkspace.Create(output);

    public void Dispose() => _workspace.Dispose();

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task PlanPinsOneSourceForBothArmsAndBoundsRepetitions()
    {
        var planDirectory = Path.Combine(_workspace.Path, "plan");
        var result = await Run("new-ci-shadow-plan.ps1",
            "-SourceSha", SourceSha, "-Repetitions", "3", "-OutputDirectory", Quote(planDirectory));

        result.EnsureSuccessful();
        using var plan = JsonDocument.Parse(File.ReadAllText(Path.Combine(planDirectory, "plan.json")));
        Assert.Equal(SourceSha, plan.RootElement.GetProperty("sourceSha").GetString());
        Assert.Equal("microsoft/aspire", plan.RootElement.GetProperty("sourceRepository").GetString());
        Assert.Equal(3, plan.RootElement.GetProperty("repetitions").GetInt32());
        Assert.Equal(s_classes.Select(c => $"Aspire.Hosting.Tests.{c}"),
            plan.RootElement.GetProperty("classes").EnumerateArray().Select(c => c.GetString()));
    }

    [Theory]
    [InlineData("-SourceSha", "main")]
    [InlineData("-SourceSha", "1111111")]
    [InlineData("-Repetitions", "0")]
    [InlineData("-Repetitions", "4")]
    [InlineData("-SourceRepository", "../outside")]
    [InlineData("-PullRequest", "1;echo")]
    [RequiresTools(["pwsh"])]
    public async Task PlanRejectsUnpinnedOrUnboundedInputs(string option, string value)
    {
        var args = new List<string> { "-OutputDirectory", Quote(Path.Combine(_workspace.Path, "plan")) };
        if (option != "-SourceSha")
        {
            args.AddRange(["-SourceSha", SourceSha]);
        }
        args.AddRange([option, value]);

        var result = await Run("new-ci-shadow-plan.ps1", [.. args]);

        Assert.NotEqual(0, result.ExitCode);
        Assert.False(Directory.Exists(Path.Combine(_workspace.Path, "plan")));
    }

    [Fact]
    [RequiresTools(["pwsh"])]
    public async Task ComparisonMatchesStableIdentitiesAndIncludesProducerAndTransferInWallSpan()
    {
        CreateEvidence();

        var result = await Compare();

        result.EnsureSuccessful();
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(_workspace.Path, "comparison.json")));
        Assert.Equal(2, summary.RootElement.GetProperty("matchedTests").GetInt32());
        Assert.Equal(110, summary.RootElement.GetProperty("lanes").GetProperty("baseline").GetProperty("executionSpanSeconds").GetDouble());
        Assert.Equal(160, summary.RootElement.GetProperty("lanes").GetProperty("payload").GetProperty("executionSpanSeconds").GetDouble());
        Assert.Equal(170, summary.RootElement.GetProperty("lanes").GetProperty("payload").GetProperty("workflowCreatedToCompletionSeconds").GetDouble());
    }

    [Theory]
    [InlineData("missing-trx", "exactly one TRX")]
    [InlineData("empty-trx", "Empty selected tests")]
    [InlineData("failed-test", "Non-passing test")]
    [InlineData("skipped-test", "Non-passing test")]
    [InlineData("different-test", "identities/outcomes differ")]
    [InlineData("wrong-class", "Unexpected test class")]
    [InlineData("duplicate-test", "Duplicate test identities")]
    [InlineData("different-source", "Provenance mismatch")]
    [InlineData("different-harness", "Provenance mismatch")]
    [InlineData("different-repetition", "Provenance mismatch")]
    [InlineData("failed-measurement", "Missing, failed, or mismatched measurement")]
    [InlineData("missing-producer", "does not exist")]
    [InlineData("failed-job", "successful completed job")]
    [RequiresTools(["pwsh"])]
    public async Task ComparisonRejectsMissingOrUnequalCoverage(string fault, string expectedError)
    {
        CreateEvidence(fault);

        var result = await Compare();

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(expectedError, result.Output);
        Assert.False(File.Exists(Path.Combine(_workspace.Path, "comparison.json")));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    [RequiresTools(["pwsh", "tar", "chmod"])]
    public async Task WorkloadBuildsOnceThenConsumerUsesPayloadWithoutMsBuild()
    {
        var result = await RunWorkload("roundtrip");

        result.EnsureSuccessful();
        using var consumer = JsonDocument.Parse(File.ReadAllText(Path.Combine(_workspace.Path, "consumer", "measurement.json")));
        Assert.Equal("success", consumer.RootElement.GetProperty("status").GetString());
        Assert.Equal(["bootstrap", "install-dotnet", "extract", "tests"],
            consumer.RootElement.GetProperty("phases").EnumerateArray().Select(p => p.GetProperty("name").GetString()));
        var calls = File.ReadAllLines(Path.Combine(_workspace.Path, "source", "calls.txt"));
        Assert.Equal(1, calls.Count(c => c == "build"));
        Assert.Equal(1, calls.Count(c => c == "msbuild"));
        Assert.Equal("test-assembly", File.ReadAllText(Path.Combine(_workspace.Path, "source", "artifacts", "bin", "Aspire.Hosting.Tests", "Debug", "net10.0", "Aspire.Hosting.Tests.dll")));
    }

    [Theory]
    [InlineData("build-failure", "build failed with exit code 17")]
    [InlineData("test-failure", "tests failed with exit code 23")]
    [InlineData("wrong-source", "Checkout is not the pinned source SHA")]
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    [RequiresTools(["pwsh", "chmod"])]
    public async Task WorkloadPropagatesFailuresAndPreservesFailedMeasurement(string scenario, string expectedError)
    {
        var result = await RunWorkload(scenario);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(expectedError, result.Output);
        using var measurement = JsonDocument.Parse(File.ReadAllText(Path.Combine(_workspace.Path, "baseline", "measurement.json")));
        Assert.Equal("failure", measurement.RootElement.GetProperty("status").GetString());
        Assert.Contains(expectedError, measurement.RootElement.GetProperty("error").GetString());
    }

    private async Task<CommandResult> RunWorkload(string scenario)
    {
        using var command = new PowerShellCommand(
            Path.Combine(RepoRoot.Path, "tests", "Infrastructure.Tests", "PowerShellScripts", "ci-shadow-workload.harness.ps1"), output)
            .WithTimeout(TimeSpan.FromMinutes(1));

        return await command.ExecuteAsync(
            "-WorkloadPath", Quote(Path.Combine(RepoRoot.Path, "eng", "scripts", "run-ci-shadow-workload.ps1")),
            "-Root", Quote(_workspace.Path), "-Scenario", scenario);
    }

    private async Task<CommandResult> Compare() => await Run("compare-ci-shadow-results.ps1",
        "-EvidenceDirectory", Quote(_workspace.Path), "-Repetition", "1",
        "-JobsPath", Quote(Path.Combine(_workspace.Path, "jobs.json")),
        "-RunPath", Quote(Path.Combine(_workspace.Path, "run.json")),
        "-OutputPath", Quote(Path.Combine(_workspace.Path, "comparison.json")));

    private async Task<CommandResult> Run(string script, params string[] args)
    {
        using var command = new PowerShellCommand(Path.Combine(RepoRoot.Path, "eng", "scripts", script), output)
            .WithTimeout(TimeSpan.FromMinutes(1));

        return await command.ExecuteAsync(args);
    }

    private void CreateEvidence(string fault = "")
    {
        foreach (var arm in new[] { "baseline", "payload" })
        {
            foreach (var className in s_classes)
            {
                var directory = Path.Combine(_workspace.Path, $"shadow-1-evidence-{arm}-{className}");
                Directory.CreateDirectory(directory);
                var target = arm == "payload" && className == s_classes[0];
                WriteMeasurement(directory, arm, className, target ? fault : "");
                var method = target && fault == "different-test" ? "OtherMethod" : "Method";
                var actualClass = target && fault == "wrong-class" ? "WrongClass" : className;
                var outcome = target && fault == "failed-test" ? "Failed" : target && fault == "skipped-test" ? "NotExecuted" : "Passed";
                var testId = Guid.NewGuid();
                var test = $"""<UnitTestResult testId="{testId}" testName="{method}" outcome="{outcome}" />""";
                var results = target && fault == "empty-trx" ? "" : target && fault == "duplicate-test" ? test + test : test;
                File.WriteAllText(Path.Combine(directory, "tests.trx"), $"""
                    <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
                      <TestDefinitions>
                        <UnitTest id="{testId}"><TestMethod className="Aspire.Hosting.Tests.{actualClass}" name="{method}" /></UnitTest>
                      </TestDefinitions>
                      <Results>{results}</Results>
                    </TestRun>
                    """);
                if (target && fault == "missing-trx")
                {
                    File.Delete(Path.Combine(directory, "tests.trx"));
                }
            }
        }
        var producer = Path.Combine(_workspace.Path, "shadow-1-evidence-producer");
        Directory.CreateDirectory(producer);
        if (fault != "missing-producer")
        {
            WriteMeasurement(producer, "producer", "");
        }
        var jobs = new List<object>();
        foreach (var className in s_classes)
        {
            jobs.Add(Job($"Baseline 1 / {className}", "00:00:10", "00:02:00"));
            jobs.Add(Job($"Payload consumer 1 / {className}", "00:02:10", "00:02:50", fault == "failed-job" ? "failure" : "success"));
        }
        jobs.Add(Job("Payload producer 1", "00:00:10", "00:01:40"));
        File.WriteAllText(Path.Combine(_workspace.Path, "jobs.json"), JsonSerializer.Serialize(new[] { new { jobs } }));
        File.WriteAllText(Path.Combine(_workspace.Path, "run.json"),
            """{"created_at":"2026-10-19T00:00:00Z","html_url":"https://github.com/radical/aspire/actions/runs/1","run_attempt":1}""");
    }

    private static object Job(string name, string start, string end, string conclusion = "success") => new
    {
        id = 1,
        name = $"compare (1) / {name}",
        started_at = $"2026-10-19T{start}Z",
        completed_at = $"2026-10-19T{end}Z",
        status = "completed",
        conclusion,
        html_url = "https://github.com/radical/aspire/actions/runs/1/job/1"
    };

    private static void WriteMeasurement(string directory, string arm, string className, string fault = "") =>
        File.WriteAllText(Path.Combine(directory, "measurement.json"), JsonSerializer.Serialize(new
        {
            sourceSha = fault == "different-source" ? new string('2', 40) : SourceSha,
            sourceRepository = "microsoft/aspire",
            harnessSha = fault == "different-harness" ? "other" : "harness",
            repetition = fault == "different-repetition" ? 2 : 1,
            status = fault == "failed-measurement" ? "failure" : "success",
            arm,
            @class = string.IsNullOrEmpty(className) ? "" : $"Aspire.Hosting.Tests.{className}"
        }));

    private static string Quote(string path) => $"\"{path}\"";
}
