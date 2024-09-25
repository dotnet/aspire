// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

// FIXME: rename to show that this is when only one nuget is installed
public class CurrentTFM_TemplateTests(ITestOutputHelper testOutput) : WorkloadTestsBase(testOutput)
{
    // private const string TargetFramework = "net9.0";

    // [Fact]
    // public async Task CanNewAndBuild()
    // {
    //     var id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net9");

    //     await using var project = await AspireProject.CreateNewTemplateProjectAsync(
    //         id,
    //         "aspire",
    //         _testOutput,
    //         buildEnvironment: BuildEnvironment.ForDefaultFramework,
    //         extraArgs: $"-f {TargetFramework}");

    //     var config = "Debug";
    //     await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
    //     await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);
    // }

    // TODO: Check for failed build
    // [Fact]
    // public async Task CannotCreateNet90()
    // {
    //     string id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net9");

    //     await using var project = await AspireProject.CreateNewTemplateProjectAsync(id, "aspire", _testOutput, buildEnvironment: BuildEnvironment.ForDefaultFramework, customHiveForTemplates: _testFixture.HomeDirectory, extraArgs: "net9.0");
    // }
}
