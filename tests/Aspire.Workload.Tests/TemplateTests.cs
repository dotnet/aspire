// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

// This class has tests that start projects on their own
public class TemplateTests : WorkloadTestsBase
{
    public TemplateTests(ITestOutputHelper testOutput)
        : base(testOutput)
    {}

    public static TheoryData<string, string> TestFrameworkTypeWithConfig()
    {
        var data = new TheoryData<string, string>();
        foreach (var testType in TestFrameworkTypes)
        {
            data.Add("Debug", testType);
            data.Add("Release", testType);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(TestFrameworkTypeWithConfig))]
    public async Task BuildAndRunStarterTemplateBuiltInTest(string config, string testType)
    {
        string id = GetNewProjectId(prefix: $"starter test.{config}-{testType.Replace(".", "_")}");
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                                            id,
                                            "aspire-starter",
                                            _testOutput,
                                            buildEnvironment: BuildEnvironment.ForDefaultFramework,
                                            extraArgs: $"-t {testType}").ConfigureAwait(false);

        await AssertTestProjectRunAsync(project.TestsProjectDirectory, testType, _testOutput, config);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BuildAndRunAspireTemplate(string config)
    {
        string id = GetNewProjectId(prefix: $"aspire_{config}");
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(id, "aspire", _testOutput, buildEnvironment: BuildEnvironment.ForDefaultFramework);

        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);

        if (BuildEnvironment.HasPlaywrightSupport)
        {
            await using var context = await CreateNewBrowserContextAsync();
            var page = await project.OpenDashboardPageAsync(context);
            await CheckDashboardHasResourcesAsync(page, []);
        }
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task StarterTemplateNewAndRunWithoutExplicitBuild(string config)
    {
        var id = GetNewProjectId(prefix: $"aspire_starter_run_{config}");
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire-starter",
            _testOutput,
            buildEnvironment: BuildEnvironment.ForDefaultFramework);

        await using var context = BuildEnvironment.HasPlaywrightSupport ? await CreateNewBrowserContextAsync() : null;
        await AssertStarterTemplateRunAsync(context, project, config, _testOutput);
    }

    [Fact]
    public async Task ProjectWithNoHTTPSRequiresExplicitOverrideWithEnvironmentVariable()
    {
        string id = GetNewProjectId(prefix: "aspire");
        // Using a copy so envvars can be modified without affecting other tests
        var testSpecificBuildEnvironment = new BuildEnvironment(BuildEnvironment.ForDefaultFramework);

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire",
            _testOutput,
            buildEnvironment: testSpecificBuildEnvironment,
            extraArgs: "--no-https").ConfigureAwait(false);

        await project.BuildAsync();
        using var buildCmd = new DotNetCommand(_testOutput, buildEnv: testSpecificBuildEnvironment, label: "first-run")
                                    .WithWorkingDirectory(project.AppHostProjectDirectory);

        var res = await buildCmd.ExecuteAsync("run");
        Assert.True(res.ExitCode != 0, $"Expected the app run to fail");
        Assert.Contains("setting must be an https address unless the 'ASPIRE_ALLOW_UNSECURED_TRANSPORT'", res.Output);

        // Run with the environment variable set
        testSpecificBuildEnvironment.EnvVars["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";
        await project.StartAppHostAsync();

        if (BuildEnvironment.HasPlaywrightSupport)
        {
            await using var context = await CreateNewBrowserContextAsync();
            var page = await project.OpenDashboardPageAsync(context);
            await CheckDashboardHasResourcesAsync(page, []).ConfigureAwait(false);
        }
    }
}
