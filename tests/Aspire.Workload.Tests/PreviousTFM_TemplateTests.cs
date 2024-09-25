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
     * also check that the default framework is as expected
     *  - add this oen for CurrentTFM also
    */
    [Theory]
    [InlineData("aspire", TestTargetFramework.Net90)]
    [InlineData("aspire", TestTargetFramework.Net80)]
    [InlineData("aspire-starter", TestTargetFramework.Net90)]
    [InlineData("aspire-starter", TestTargetFramework.Net80)]
    public async Task CanNewAndBuild(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+{tfm.ToTFMString()}");

        var buildEnvToUse = tfm == TestTargetFramework.Net90 ? BuildEnvironment.ForNet90 : BuildEnvironment.ForNet80;
        var templateHive = tfm == TestTargetFramework.Net90 ? TemplatesCustomHive.Net9_0_Net9 : TemplatesCustomHive.Net9_0_Net8;
        await templateHive.Value.InstallAsync(
            BuildEnvironment.GetNewTemplateCustomHiveDefaultDirectory(),
            buildEnvToUse.BuiltNuGetsPath,
            buildEnvToUse.DotNet);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            extraArgs: $"-f {tfm.ToTFMString()}",
            customHiveForTemplates: templateHive.Value.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
        // await project.StartAppHostAsync(extraArgs: [$"-c {config}"]);
    }

    // TODO: Check for failed build
    // [Fact]
    // public async Task CannotCreateNet90()
    // {
    //     string id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net9");

    //     await using var project = await AspireProject.CreateNewTemplateProjectAsync(id, "aspire", _testOutput, buildEnvironment: BuildEnvironment.ForDefaultFramework, customHiveForTemplates: _testFixture.HomeDirectory, extraArgs: "net9.0");
    // }
}
