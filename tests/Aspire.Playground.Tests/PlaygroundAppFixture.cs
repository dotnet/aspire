// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;
using Aspire.Workload.Tests;

namespace Aspire.Playground.Tests;

public class PlaygroundAppFixture : IAsyncLifetime
{
#pragma warning disable CS0649
#pragma warning disable CA1802
#if TESTS_RUNNING_OUTSIDE_OF_REPO
    private static readonly bool s_testsRunningOutsideOfRepo = true;
#else
    private static readonly bool s_testsRunningOutsideOfRepo;
#endif
#pragma warning restore CS0649
#pragma warning restore CA1802

    public static string? TestScenario { get; } = EnvironmentVariables.TestScenario;
    public string PlaygroundAppsPath { get; set; }
    public Dictionary<string, ProjectInfo> Projects => Project?.InfoTable ?? throw new InvalidOperationException("Project is not initialized");

    private readonly string _relativeAppHostProjectDir;

    // private TestResourceNames _resourcesToSkip;
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TestOutputWrapper _testOutput;
    private AspireProject? _project;

    public BuildEnvironment BuildEnvironment { get; init; }
    public AspireProject Project => _project ?? throw new InvalidOperationException("Project is not initialized");

    public PlaygroundAppFixture(string relativeAppHostProjectDir, IMessageSink diagnosticMessageSink)
    {
        _relativeAppHostProjectDir = relativeAppHostProjectDir;
        _diagnosticMessageSink = diagnosticMessageSink;
        _testOutput = new TestOutputWrapper(messageSink: _diagnosticMessageSink);

        BuildEnvironment = new(useSystemDotNet: !s_testsRunningOutsideOfRepo);
        if (s_testsRunningOutsideOfRepo)
        {
            if (!BuildEnvironment.HasWorkloadFromArtifacts)
            {
                throw new InvalidOperationException("Expected to have sdk+workload from artifacts when running tests outside of the repo");
            }

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

            PlaygroundAppsPath = Path.Combine(BuildEnvironment.RepoRoot.FullName, "playground");
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
        if (s_testsRunningOutsideOfRepo)
        {
            _testOutput.WriteLine("");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine($"   Running Playground tests outside-of-repo");
            _testOutput.WriteLine($"   Playground project: {Project.AppHostProjectDirectory}");
            _testOutput.WriteLine($"****************************************");
            _testOutput.WriteLine("");
        }

        await Project.BuildAsync();

        await Project.StartAppHostAsync();

        foreach (var project in Projects.Values)
        {
            project.Client = AspireProject.Client.Value;
        }
    }

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

