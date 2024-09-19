// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class StarterTemplateProjectNamesTests : WorkloadTestsBase
{
    public StarterTemplateProjectNamesTests(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    public static TheoryData<string, string> ProjectNamesWithTestType_TestData()
    {
        var data = new TheoryData<string, string>();
        foreach (var testType in TestFrameworkTypes)
        {
            foreach (var name in GetProjectNamesForTest())
            {
                data.Add(name, testType);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestType_TestData))]
    public async Task StarterTemplateWithTest_ProjectNames(string prefix, string testType)
    {
        string id = $"{prefix}-{testType}";
        string config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire-starter",
            _testOutput,
            BuildEnvironment.ForDefaultFramework,
            $"-t {testType}");

        await using var context = PlaywrightProvider.HasPlaywrightSupport ? await CreateNewBrowserContextAsync() : null;
        _testOutput.WriteLine($"Checking the starter template project");
        await AssertStarterTemplateRunAsync(context, project, config, _testOutput);

        _testOutput.WriteLine($"Checking the starter template project tests");
        await AssertTestProjectRunAsync(project.TestsProjectDirectory, testType, _testOutput, config);
    }
}
