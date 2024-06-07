// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Aspire.TestProject;
using Aspire.Workload.Tests;

namespace Aspire.EndToEnd.Tests;

/// <summary>
/// This fixture ensures the TestProject.AppHost application is started before a test is executed.
///
/// Represents the the IntegrationServiceA project in the test application used to send HTTP requests
/// to the project's endpoints.
/// </summary>
public sealed class IntegrationServicesFixture : IAsyncLifetime
{
#if TESTS_RUNNING_OUTSIDE_OF_REPO
    public static bool TestsRunningOutsideOfRepo = true;
#else
    public static bool TestsRunningOutsideOfRepo;
#endif

    public static string? TestScenario { get; } = EnvironmentVariables.TestScenario;
    public Dictionary<string, ProjectInfo> Projects => Project?.InfoTable ?? throw new InvalidOperationException("Project is not initialized");
    private TestResourceNames _resourcesToSkip;
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TestOutputWrapper _testOutput;
    private AspireProject? _project;

    public BuildEnvironment BuildEnvironment { get; init; }
    public ProjectInfo IntegrationServiceA => Projects["integrationservicea"];
    public AspireProject Project => _project ?? throw new InvalidOperationException("Project is not initialized");

    public IntegrationServicesFixture(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink);
        BuildEnvironment = new(useSystemDotNet: !TestsRunningOutsideOfRepo);
        if (TestsRunningOutsideOfRepo)
        {
            if (!BuildEnvironment.HasWorkloadFromArtifacts)
            {
                throw new InvalidOperationException("Expected to have sdk+workload from artifacts when running tests outside of the repo");
            }
            BuildEnvironment.EnvVars["TestsRunningOutsideOfRepo"] = "true";
        }
        else
        {
            // inside the repo
            if (BuildEnvironment.RepoRoot is null)
            {
                throw new InvalidOperationException("These tests should be run from inside the repo when using `TestsRunningOutsideOfRepo=false`");
            }

            BuildEnvironment.TestAssetsPath = Path.Combine(BuildEnvironment.RepoRoot.FullName, "tests");
            if (!Directory.Exists(BuildEnvironment.TestAssetsPath))
            {
                throw new ArgumentException($"Cannot find TestAssetsPath={BuildEnvironment.TestAssetsPath}");
            }
        }
    }

    public async Task InitializeAsync()
    {
        string testProjectPath = Path.Combine(BuildEnvironment.TestAssetsPath, "testproject");
        _project = new AspireProject("TestProject", testProjectPath, _testOutput, BuildEnvironment);
        if (TestsRunningOutsideOfRepo)
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine($"   Running EndToEnd tests outside-of-repo");
            _testOutput.WriteLine($"   TestProject: {Project.AppHostProjectDirectory}");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine("");
        }

        await Project.BuildAsync();

        string extraArgs = "";
        _resourcesToSkip = GetResourcesToSkip();
        if (_resourcesToSkip != TestResourceNames.None && _resourcesToSkip.ToCSVString() is string skipArg)
        {
            extraArgs += $"--skip-resources {skipArg}";
        }
        await Project.StartAppHostAsync([extraArgs]);

        foreach (var project in Projects.Values)
        {
            project.Client = AspireProject.Client.Value;
        }
    }

    public Task DumpComponentLogsAsync(TestResourceNames resource, ITestOutputHelper? testOutputArg = null)
    {
        if (resource == TestResourceNames.None)
        {
            return Task.CompletedTask;
        }
        if (resource == TestResourceNames.All || !Enum.IsDefined<TestResourceNames>(resource))
        {
            throw new ArgumentException($"Only one resource is supported at a time. resource: {resource}");
        }

        string component = resource switch
        {
            TestResourceNames.cosmos => "cosmos",
            TestResourceNames.kafka => "kafka",
            TestResourceNames.mongodb => "mongodb",
            TestResourceNames.mysql or TestResourceNames.efmysql => "mysql",
            TestResourceNames.oracledatabase => "oracledatabase",
            TestResourceNames.postgres or TestResourceNames.efnpgsql => "postgres",
            TestResourceNames.rabbitmq => "rabbitmq",
            TestResourceNames.redis => "redis",
            TestResourceNames.garnet => "garnet",
            TestResourceNames.valkey => "valkey",
            TestResourceNames.sqlserver => "sqlserver",
            TestResourceNames.milvus => "milvus",
            TestResourceNames.eventhubs => "eventhubs",
            _ => throw new ArgumentException($"Unknown resource: {resource}")
        };

        return Project.DumpComponentLogsAsync(component, testOutputArg);
    }

    public async Task DisposeAsync()
    {
        if (_project is not null)
        {
            await _project.DisposeAsync();
        }
    }

    public void EnsureAppHasResources(TestResourceNames expectedResourceNames)
    {
        foreach (var ename in Enum.GetValues<TestResourceNames>())
        {
            if (ename != TestResourceNames.None && expectedResourceNames.HasFlag(ename) && _resourcesToSkip.HasFlag(ename))
            {
                throw new InvalidOperationException($"The required resource '{ename}' was skipped for the app run for TestScenario: {TestScenario}. Make sure that the TEST_SCENARIO environment variable matches the intended scenario for the test. Resources that were skipped: {string.Join(",", _resourcesToSkip)}. TestScenario: {TestScenario} ");
            }
        }
    }

    private static TestResourceNames GetResourcesToSkip()
    {
        TestResourceNames resourcesToInclude = TestScenario switch
        {
            "oracle" => TestResourceNames.oracledatabase,
            "cosmos" => TestResourceNames.cosmos,
            "eventhubs" => TestResourceNames.eventhubs,
            "basicservices" => TestResourceNames.kafka
                              | TestResourceNames.mongodb
                              | TestResourceNames.rabbitmq
                              | TestResourceNames.redis
                              | TestResourceNames.garnet
                              | TestResourceNames.valkey
                              | TestResourceNames.postgres
                              | TestResourceNames.efnpgsql
                              | TestResourceNames.mysql
                              | TestResourceNames.efmysql
                              | TestResourceNames.sqlserver
                              | TestResourceNames.milvus,
            "" or null => TestResourceNames.All,
            _ => throw new ArgumentException($"Unknown test scenario '{TestScenario}'")
        };

        TestResourceNames resourcesToSkip = TestResourceNames.All & ~resourcesToInclude;

        // always skip cosmos on macos/arm64
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            resourcesToSkip |= TestResourceNames.cosmos;
        }
        if (string.IsNullOrEmpty(TestScenario))
        {
            // no scenario specified
            if (BuildEnvironment.IsRunningOnCI)
            {
                resourcesToSkip |= TestResourceNames.cosmos;
                resourcesToSkip |= TestResourceNames.oracledatabase;
            }
        }

        // always skip the dashboard
        resourcesToSkip |= TestResourceNames.dashboard;

        return resourcesToSkip;
    }
}
