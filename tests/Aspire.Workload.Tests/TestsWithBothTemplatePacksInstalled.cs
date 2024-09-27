// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class TestsWithBothTemplatePacksInstalled : WorkloadTestsBase
{
    private readonly TemplatesCustomHive _templateHive;

    public TestsWithBothTemplatePacksInstalled(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _templateHive = TemplatesCustomHive.With9_0_Net9_And_Net8;
    }

    [Theory]
    [InlineData("aspire", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-starter", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-starter", TestTargetFramework.PreviousTFM)]
    public async Task CanNewAndBuildSolutionTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.CurrentTFM ? BuildEnvironment.ForCurrentSdk : BuildEnvironment.ForPreviousSdk;
        await _templateHive.InstallAsync(buildEnvToUse);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            targetFramework: tfm,
            addEndpointsHook: templateName is "aspire" or "aspire-starter",
            customHiveForTemplates: _templateHive.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
    }

    [Theory]
    [InlineData("aspire-apphost", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-apphost", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-mstest", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-mstest", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-xunit", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-xunit", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-nunit", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-nunit", TestTargetFramework.PreviousTFM)]
    public async Task CanNewAndBuildStandaloneProjectTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.CurrentTFM ? BuildEnvironment.ForCurrentSdk : BuildEnvironment.ForPreviousSdk;
        await _templateHive.InstallAsync(buildEnvToUse);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            targetFramework: tfm,
            addEndpointsHook: templateName is "aspire" or "aspire-starter",
            customHiveForTemplates: _templateHive.CustomHiveDirectory);

        string config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"], workingDirectory: project.RootDir);
    }
}
