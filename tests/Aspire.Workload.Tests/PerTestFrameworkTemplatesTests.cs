// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public abstract partial class PerTestFrameworkTemplatesTests : WorkloadTestsBase
{
    private readonly string _testTemplateName;

    public PerTestFrameworkTemplatesTests(string testType, ITestOutputHelper testOutput) : base(testOutput)
    {
        _testTemplateName = testType;
    }

    public static IEnumerable<object[]> ProjectNamesWithTestTemplate_TestData()
    {
        foreach (var name in GetProjectNamesForTest())
        {
            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestTemplate_TestData))]
    public async Task TemplatesForIndividualTestFrameworks(string prefix)
    {
        var id = $"{prefix}-{_testTemplateName}";
        var config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire",
            _testOutput,
            buildEnvironment: BuildEnvironment.ForDefaultFramework);

        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
        if (PlaywrightProvider.HasPlaywrightSupport)
        {
            await using (var context = await CreateNewBrowserContextAsync())
            {
                await AssertBasicTemplateAsync(context);
            }
        }

        var testProjectDir = await CreateAndAddTestTemplateProjectAsync(id, _testTemplateName, project);

        using var cmd = new DotNetCommand(_testOutput, label: $"test-{_testTemplateName}")
                        .WithWorkingDirectory(testProjectDir)
                        .WithTimeout(TimeSpan.FromMinutes(3));

        var res = await cmd.ExecuteAsync($"test -c {config}");

        Assert.True(res.ExitCode != 0, $"Expected the tests project run to fail");
        Assert.Matches("System.ArgumentException.*Resource 'webfrontend' not found.", res.Output);
        Assert.Matches("Failed! * - Failed: *1, Passed: *0, Skipped: *0, Total: *1", res.Output);

        async Task AssertBasicTemplateAsync(IBrowserContext context)
        {
            await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);

            try
            {
                var page = await project.OpenDashboardPageAsync(context);
                await CheckDashboardHasResourcesAsync(page, []);
            }
            finally
            {
                await project.StopAppHostAsync();
            }
        }
    }
}

// Individual class for each test framework so the tests can run in separate helix jobs
public class MSTest_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public MSTest_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-mstest", testOutput)
    {
    }
}

public class Xunit_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public Xunit_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-xunit", testOutput)
    {
    }
}

public class Nunit_PerTestFrameworkTemplatesTests : PerTestFrameworkTemplatesTests
{
    public Nunit_PerTestFrameworkTemplatesTests(ITestOutputHelper testOutput) : base("aspire-nunit", testOutput)
    {
    }
}
