// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

// This class has tests that start projects on their own
public class TemplateTests : WorkloadTestsBase
{
    public TemplateTests(ITestOutputHelper testOutput)
        : base(testOutput)
    {}

    [Theory]
    [InlineData("Debug", "none")]
    [InlineData("Debug", "mstest")]
    [InlineData("Debug", "nunit")]
    [InlineData("Debug", "xunit.net")]
    [InlineData("Release", "none")]
    [InlineData("Release", "mstest")]
    [InlineData("Release", "nunit")]
    [InlineData("Release", "xunit.net")]
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

        await using var context = await CreateNewBrowserContextAsync();
        var page = await project.OpenDashboardPageAsync(context);
        await CheckDashboardHasResourcesAsync(page, []);
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

        await using var context = await CreateNewBrowserContextAsync();
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

        await using var context = await CreateNewBrowserContextAsync();
        var page = await project.OpenDashboardPageAsync(context);
        await CheckDashboardHasResourcesAsync(page, []).ConfigureAwait(false);
    }

    private static async Task AssertStarterTemplateRunAsync(IBrowserContext context, AspireProject project, string config, ITestOutputHelper _testOutput)
    {
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"], noBuild: false);

        var page = await project.OpenDashboardPageAsync(context);
        ResourceRow[] resourceRows;
        try
        {
            resourceRows = await CheckDashboardHasResourcesAsync(
                                    page,
                                    StarterTemplateRunTestsBase<StarterTemplateFixture>.GetExpectedResources(project, hasRedisCache: false),
                                    _testOutput).ConfigureAwait(false);
        }
        catch
        {
            string screenshotPath = Path.Combine(project.LogPath, "dashboard-fail.png");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
            _testOutput.WriteLine($"Dashboard screenshot saved to {screenshotPath}");
            throw;
        }

        string url = resourceRows.First(r => r.Name == "webfrontend").Endpoints[0];
        await StarterTemplateRunTestsBase<StarterTemplateFixture>.CheckWebFrontendWorksAsync(context, url, _testOutput, project.LogPath);
        await project.StopAppHostAsync();
    }

}
