// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class TestsWithBothTemplatePacksInstalled : WorkloadTestsBase
{
    public TestsWithBothTemplatePacksInstalled(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Theory]
    [InlineData("aspire", TestTargetFramework.Net90)]
    [InlineData("aspire", TestTargetFramework.Net80)]
    [InlineData("aspire-starter", TestTargetFramework.Net90)]
    [InlineData("aspire-starter", TestTargetFramework.Net80)]
    public async Task CanNewAndBuildSolutionTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.Net90 ? BuildEnvironment.ForNet90 : BuildEnvironment.ForNet80;
        var templateHive = TemplatesCustomHive.Net9_0_Net8_And_Net9;
        await templateHive.InstallAsync(
            BuildEnvironment.GetNewTemplateCustomHiveDefaultDirectory(),
            buildEnvToUse.BuiltNuGetsPath,
            buildEnvToUse.DotNet);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            extraArgs: $"-f {tfm.ToTFMString()}",
            addEndpointsHook: templateName is "aspire" or "aspire-starter",
            customHiveForTemplates: templateHive.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
    }

    [Theory]
    [InlineData("aspire-apphost", TestTargetFramework.Net90)]
    [InlineData("aspire-apphost", TestTargetFramework.Net80)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.Net90)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.Net80)]
    [InlineData("aspire-mstest", TestTargetFramework.Net90)]
    [InlineData("aspire-mstest", TestTargetFramework.Net80)]
    [InlineData("aspire-xunit", TestTargetFramework.Net90)]
    [InlineData("aspire-xunit", TestTargetFramework.Net80)]
    [InlineData("aspire-nunit", TestTargetFramework.Net90)]
    [InlineData("aspire-nunit", TestTargetFramework.Net80)]
    public async Task CanNewAndBuildStandaloneProjectTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.Net90 ? BuildEnvironment.ForNet90 : BuildEnvironment.ForNet80;
        var templateHive = TemplatesCustomHive.Net9_0_Net8_And_Net9;
        await templateHive.InstallAsync(
            BuildEnvironment.GetNewTemplateCustomHiveDefaultDirectory(),
            buildEnvToUse.BuiltNuGetsPath,
            buildEnvToUse.DotNet);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            extraArgs: $"-f {tfm.ToTFMString()}",
            addEndpointsHook: templateName is "aspire" or "aspire-starter",
            customHiveForTemplates: templateHive.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"], workingDirectory: project.RootDir);
    }
}
