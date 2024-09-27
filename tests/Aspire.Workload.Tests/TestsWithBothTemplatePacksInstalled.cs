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
    [InlineData("aspire", TestTargetFramework.Current)]
    [InlineData("aspire", TestTargetFramework.Previous)]
    [InlineData("aspire-starter", TestTargetFramework.Current)]
    [InlineData("aspire-starter", TestTargetFramework.Previous)]
    public async Task CanNewAndBuildSolutionTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.Current ? BuildEnvironment.ForCurrentSdk : BuildEnvironment.ForPreviousSdk;
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
    [InlineData("aspire-apphost", TestTargetFramework.Current)]
    [InlineData("aspire-apphost", TestTargetFramework.Previous)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.Current)]
    [InlineData("aspire-servicedefaults", TestTargetFramework.Previous)]
    [InlineData("aspire-mstest", TestTargetFramework.Current)]
    [InlineData("aspire-mstest", TestTargetFramework.Previous)]
    [InlineData("aspire-xunit", TestTargetFramework.Current)]
    [InlineData("aspire-xunit", TestTargetFramework.Previous)]
    [InlineData("aspire-nunit", TestTargetFramework.Current)]
    [InlineData("aspire-nunit", TestTargetFramework.Previous)]
    public async Task CanNewAndBuildStandaloneProjectTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_{templateName}_{tfm}_on_9+8");

        var buildEnvToUse = tfm == TestTargetFramework.Current ? BuildEnvironment.ForCurrentSdk : BuildEnvironment.ForPreviousSdk;
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
