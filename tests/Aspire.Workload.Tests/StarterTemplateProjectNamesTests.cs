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

    public static TheoryData<string> ProjectNamesWithTestType_TestData()
    {
        var data = new TheoryData<string>();
        foreach (var name in GetProjectNamesForTest())
        {
            data.Add(name);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestType_TestData))]
    public async Task StarterTemplateWithTest_ProjectNames(string id)
    {
        string config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire-starter",
            _testOutput,
            BuildEnvironment.ForDefaultFramework,
            $"-t").ConfigureAwait(false);

        await using var context = await CreateNewBrowserContextAsync();
        _testOutput.WriteLine($"Checking the starter template project");
        await AssertStarterTemplateRunAsync(context, project, config, _testOutput).ConfigureAwait(false);

        _testOutput.WriteLine($"Checking the starter template project tests");
        await AssertTestProjectRunAsync(project.TestsProjectDirectory, _testOutput, config).ConfigureAwait(false);
    }
}
