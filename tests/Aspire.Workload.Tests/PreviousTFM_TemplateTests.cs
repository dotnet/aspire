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
     *



     * With both installed
        * - can create and build net8, and net9
    * with 9 installed
        [x] can create and build net9
        [ ] cannot create net8
    * with net8 installed (and 8 sdk)
        [x] can create and build net8
        [ ] cannot create net9

      Current:

    */
    [Theory]
    [InlineData("aspire", TestTargetFramework.Net90)]
    [InlineData("aspire", TestTargetFramework.Net80)]
    [InlineData("aspire-starter", TestTargetFramework.Net90)]
    [InlineData("aspire-starter", TestTargetFramework.Net80)]
    public async Task CanNewAndBuildWithMatchingTemplatePackInstalled(string templateName, TestTargetFramework tfm)
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

    // TODO: Separate class for WithBothTemplatePacksInstalled?
    [Theory]
    [InlineData("aspire", TestTargetFramework.Net90)]
    [InlineData("aspire", TestTargetFramework.Net80)]
    [InlineData("aspire-starter", TestTargetFramework.Net90)]
    [InlineData("aspire-starter", TestTargetFramework.Net80)]
    public async Task CanNewAndBuildWithBothTemplatePacksInstalled(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.Net90 ? BuildEnvironment.ForNet90 : BuildEnvironment.ForNet80;
        var templateHive = TemplatesCustomHive.Net9_0_Net8_And_Net9;
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

    [Fact]
    public async Task CannotCreateNet90()
    {
        string templateName = "aspire";
        var tfm = TestTargetFramework.Net90;
        string id = GetNewProjectId(prefix: $"new_build_{TargetFramework}_on_9+net9");

        var buildEnvToUse = tfm == TestTargetFramework.Net90 ? BuildEnvironment.ForNet90 : BuildEnvironment.ForNet80;
        var templateHive = tfm == TestTargetFramework.Net90 ? TemplatesCustomHive.Net9_0_Net8 : TemplatesCustomHive.Net9_0_Net9;
        await templateHive.Value.InstallAsync(
            BuildEnvironment.GetNewTemplateCustomHiveDefaultDirectory(),
            buildEnvToUse.BuiltNuGetsPath,
            buildEnvToUse.DotNet);

        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id,
                templateName,
                _testOutput,
                buildEnvironment: buildEnvToUse,
                extraArgs: $"-f {tfm.ToTFMString()}",
                customHiveForTemplates: templateHive.Value.CustomHiveDirectory);
        }
        catch (ToolCommandException tce)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains($"'{tfm.ToTFMString()}' is not a valid value for -f", tce.Result.Value.Output);
        }
    }
}
