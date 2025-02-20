// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public abstract class StarterTemplateProjectNamesTests : WorkloadTestsBase
{
    private readonly string _testType;
    public StarterTemplateProjectNamesTests(string testType, ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _testType = testType;
    }

    public static IEnumerable<object[]> ProjectNamesWithTestType_TestData()
    {
        foreach (var name in GetProjectNamesForTest())
        {
            yield return [name];
        }
    }

    [Theory]
    [MemberData(nameof(ProjectNamesWithTestType_TestData))]
    public async Task StarterTemplateWithTest_ProjectNames(string prefix)
    {
        string id = $"{prefix}-{_testType}";
        string config = "Debug";

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire-starter",
            _testOutput,
            BuildEnvironment.ForDefaultFramework,
            $"-t {_testType}");

        await using var context = PlaywrightProvider.HasPlaywrightSupport ? await CreateNewBrowserContextAsync() : null;
        _testOutput.WriteLine($"Checking the starter template project");
        await AssertStarterTemplateRunAsync(context, project, config, _testOutput);

        _testOutput.WriteLine($"Checking the starter template project tests");
        await AssertTestProjectRunAsync(project.TestsProjectDirectory, _testType, _testOutput, config);
    }
}

// Individual class for each test framework so the tests can run in separate helix jobs
public class MSTest_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public MSTest_StarterTemplateProjectNamesTests(ITestOutputHelper testOutput) : base("aspire-mstest", testOutput)
    {
    }
}

public class Xunit_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public Xunit_StarterTemplateProjectNamesTests(ITestOutputHelper testOutput) : base("aspire-xunit", testOutput)
    {
    }
}

public class Nunit_StarterTemplateProjectNamesTests : StarterTemplateProjectNamesTests
{
    public Nunit_StarterTemplateProjectNamesTests(ITestOutputHelper testOutput) : base("aspire-nunit", testOutput)
    {
    }
}
