// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class PreviousTFM_TemplateTests : WorkloadTestsBase, IClassFixture<DotNet_With9_Net8_Fixture>
{
    // private readonly DotNet_With9_Net8_Fixture _testFixture;
    private const string TargetFramework = "net8.0";
    private readonly DotNet_With9_Net8_Fixture _testFixture;

    public PreviousTFM_TemplateTests(DotNet_With9_Net8_Fixture fixture, ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _testFixture = fixture;
    }

    // FIXME: new+build tests
    /*
     * aspire-starter
     * aspire-starter with tests
     * aspire
    */
    [Fact]
    public async Task CanNewAndBuild()
    {
        string id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net8");

        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            "aspire",
            _testOutput,
            buildEnvironment: BuildEnvironment.ForDefaultFramework,
            extraArgs: $"-f {TargetFramework}",
            customHiveForTemplates: _testFixture.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
        await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);
    }

    // TODO: Check for failed build
    // [Fact]
    // public async Task CannotCreateNet90()
    // {
    //     string id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net9");

    //     await using var project = await AspireProject.CreateNewTemplateProjectAsync(id, "aspire", _testOutput, buildEnvironment: BuildEnvironment.ForDefaultFramework, customHiveForTemplates: _testFixture.HomeDirectory, extraArgs: "net9.0");
    // }
}
