// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using Aspire.Workload.Tests;

namespace Aspire.Playground.Tests;

public class PlaygroundAppFixture : IAsyncLifetime
{
#if TESTS_RUNNING_OUTSIDE_OF_REPO
    private const bool TestsRunningOutsideOfRepo = true;
#else
    private const bool TestsRunningOutsideOfRepo = false;
#endif

    // private static readonly bool s_testsRunningOutsideOfRepo = Environment.GetEnvironmentVariable("TESTS_RUNNING_OUTSIDE_OF_REPO") is "true";

    public static string? TestScenario { get; } = EnvironmentVariables.TestScenario;
    public string PlaygroundAppsPath { get; set; }
    public Dictionary<string, ProjectInfo> Projects => Project?.InfoTable ?? throw new InvalidOperationException("Project is not initialized");

    private readonly string _relativeAppHostProjectDir;

    // private TestResourceNames _resourcesToSkip;
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TestOutputWrapper _testOutput;
    private readonly bool _testsRunningOutsideOfRepo;
    private AspireProject? _project;

    public BuildEnvironment BuildEnvironment { get; init; }
    // public ProjectInfo IntegrationServiceA => Projects["integrationservicea"];
    public AspireProject Project => _project ?? throw new InvalidOperationException("Project is not initialized");

    public PlaygroundAppFixture(string relativeAppHostProjectDir, IMessageSink diagnosticMessageSink)
    {
        _relativeAppHostProjectDir = relativeAppHostProjectDir;
        _diagnosticMessageSink = diagnosticMessageSink;
        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink);

        // var repoRoot = TestUtils.FindRepoRoot();
        // _testsRunningOutsideOfRepo = repoRoot is null;
        _testsRunningOutsideOfRepo = TestsRunningOutsideOfRepo;
        BuildEnvironment = new(useSystemDotNet: !_testsRunningOutsideOfRepo);
        if (_testsRunningOutsideOfRepo)
        {
            if (!BuildEnvironment.HasWorkloadFromArtifacts)
            {
                throw new InvalidOperationException("Expected to have sdk+workload from artifacts when running tests outside of the repo");
            }

            // if (EnvironmentVariables.PlaygroundAppsPath is null)
            // {
            //     PlaygroundAppsPath = Path.Combine(AppContext.BaseDirectory, "archive", "playground");
            //     if (!Directory.Exists(PlaygroundAppsPath))
            //     {
            //         throw new InvalidOperationException($"Expected to have the PLAYGROUND_APPS_PATH environment variable set when running tests outside of the repo, or to find the playground apps in the default location in {PlaygroundAppsPath}");
            //     }
            // }
            // else
            // {
            //     PlaygroundAppsPath = EnvironmentVariables.PlaygroundAppsPath;
            // }
            PlaygroundAppsPath = Path.Combine(BuildEnvironment.TestAssetsPath, "playground");
            if (!Directory.Exists(PlaygroundAppsPath))
            {
                throw new ArgumentException($"Cannot find PlaygroundAppsPath={PlaygroundAppsPath} under testassets {BuildEnvironment.TestAssetsPath}");
            }

            BuildEnvironment.EnvVars["TestsRunningOutsideOfRepo"] = "true";
            BuildEnvironment.EnvVars["PackageVersion"] = BuildEnvironment.IsRunningOnCI ? "8.1.0-ci" : "8.1.0-dev";
            BuildEnvironment.EnvVars["TestAssetsDir"] = BuildEnvironment.TestAssetsPath + "/";

            // maps to src/Shared
            BuildEnvironment.EnvVars["SharedDir"] = Path.GetFullPath(Path.Combine(BuildEnvironment.TestAssetsPath, "Shared-src")) + "/";
        }
        else
        {
            // inside the repo
            if (BuildEnvironment.RepoRoot is null)
            {
                throw new InvalidOperationException("These tests should be run from inside the repo when using `TestsRunningOutsideOfRepo=false`");
            }

            // BuildEnvironment.TestAssetsPath = Path.Combine(BuildEnvironment.RepoRoot.FullName, "tests-xy");
            // if (!Directory.Exists(BuildEnvironment.TestAssetsPath))
            // {
            //     throw new ArgumentException($"Cannot find TestAssetsPath={BuildEnvironment.TestAssetsPath}");
            // }
            PlaygroundAppsPath = Path.Combine(BuildEnvironment.RepoRoot.FullName, "playground");
            // PlaygroundAppsPath = Path.Combine(AppContext.BaseDirectory, "archive", "playground");
            if (!Directory.Exists(PlaygroundAppsPath))
            {
                throw new ArgumentException($"Cannot find PlaygroundAppsPath={PlaygroundAppsPath}");
            }
        }

        BuildEnvironment.EnvVars["BuildForTest"] = "true";
    }

    public async Task InitializeAsync()
    {
        string pgProjectDir = Path.Combine(PlaygroundAppsPath, _relativeAppHostProjectDir);
        if (!Directory.Exists(pgProjectDir))
        {
            throw new DirectoryNotFoundException($"Playground project directory not found: {pgProjectDir}");
        }
        _project = new AspireProject(Path.GetFileName(_relativeAppHostProjectDir),
                                     baseDir: Path.Combine(pgProjectDir, ".."),
                                     _testOutput,
                                     BuildEnvironment,
                                     relativeAppHostProjectDir: pgProjectDir);
        if (_testsRunningOutsideOfRepo)
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine($"   Running EndToEnd tests outside-of-repo");
            _testOutput.WriteLine($"   Playground project: {Project.AppHostProjectDirectory}");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine("");
        }

        await Project.BuildAsync();

        string extraArgs = "";
        // _resourcesToSkip = GetResourcesToSkip();
        // if (_resourcesToSkip != TestResourceNames.None && _resourcesToSkip.ToCSVString() is string skipArg)
        // {
        //     extraArgs += $"--skip-resources {skipArg}";
        // }
        await Project.StartAppHostAsync([extraArgs]);

        foreach (var project in Projects.Values)
        {
            project.Client = AspireProject.Client.Value;
        }
    }

    // public Task DumpComponentLogsAsync(TestResourceNames resource, ITestOutputHelper? testOutputArg = null)
    // {
    //     if (resource == TestResourceNames.None)
    //     {
    //         return Task.CompletedTask;
    //     }
    //     if (resource == TestResourceNames.All || !Enum.IsDefined<TestResourceNames>(resource))
    //     {
    //         throw new ArgumentException($"Only one resource is supported at a time. resource: {resource}");
    //     }

    //     string component = resource switch
    //     {
    //         TestResourceNames.cosmos => "cosmos",
    //         TestResourceNames.kafka => "kafka",
    //         TestResourceNames.mongodb => "mongodb",
    //         TestResourceNames.mysql or TestResourceNames.efmysql => "mysql",
    //         TestResourceNames.oracledatabase => "oracledatabase",
    //         TestResourceNames.postgres or TestResourceNames.efnpgsql => "postgres",
    //         TestResourceNames.rabbitmq => "rabbitmq",
    //         TestResourceNames.redis => "redis",
    //         TestResourceNames.garnet => "garnet",
    //         TestResourceNames.valkey => "valkey",
    //         TestResourceNames.sqlserver => "sqlserver",
    //         TestResourceNames.milvus => "milvus",
    //         TestResourceNames.eventhubs => "eventhubs",
    //         _ => throw new ArgumentException($"Unknown resource: {resource}")
    //     };

    //     return Project.DumpComponentLogsAsync(component, testOutputArg);
    // }

    public async Task DisposeAsync()
    {
        if (_project is not null)
        {
            await _project.DisposeAsync();
        }
    }
}

public sealed class MongoPlaygroundAppFixture : PlaygroundAppFixture
{
    public MongoPlaygroundAppFixture(IMessageSink diagnosticMessageSink)
        : base ("mongo/Mongo.AppHost", diagnosticMessageSink)
    {
    }
}

public sealed class MysqlPlaygroundAppFixture : PlaygroundAppFixture
{
    public MysqlPlaygroundAppFixture(IMessageSink diagnosticMessageSink)
        : base ("mysql/MySqlDb.AppHost", diagnosticMessageSink)
    {
    }
}

public sealed class NatsPlaygroundAppFixture : PlaygroundAppFixture
{
    public NatsPlaygroundAppFixture(IMessageSink diagnosticMessageSink)
        : base ("nats/Nats.AppHost", diagnosticMessageSink)
    {
    }
}

public sealed class DaprPlaygroundAppFixture : PlaygroundAppFixture
{
    public DaprPlaygroundAppFixture(IMessageSink diagnosticMessageSink)
        : base ("dapr/AppHost", diagnosticMessageSink)
    {
    }
}

