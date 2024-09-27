// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class PreviousTFM_TemplateTests : WorkloadTestsBase
{
    public PreviousTFM_TemplateTests(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Theory]
    [InlineData("aspire", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-starter", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-starter", TestTargetFramework.PreviousTFM)]
    public async Task CanNewAndBuildWithMatchingTemplatePackInstalled(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{tfm.ToTFMString()}_on_9+{tfm.ToTFMString()}");

        var buildEnvToUse = tfm == TestTargetFramework.CurrentTFM
                                ? BuildEnvironment.ForCurrentTFM
                                : BuildEnvironment.ForPreviousTFM;
        var templateHive = tfm == TestTargetFramework.CurrentTFM
                                ? TemplatesCustomHive.With9_0_Net9
                                : TemplatesCustomHive.With9_0_Net8;
        await templateHive.InstallAsync(buildEnvToUse);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            targetFramework: tfm,
            customHiveForTemplates: templateHive.CustomHiveDirectory);

        var config = "Debug";
        await project.BuildAsync(extraBuildArgs: [$"-c {config}"]);
    }

    [Theory]
    [InlineData("aspire", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-starter", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-starter", TestTargetFramework.PreviousTFM)]
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
    public async Task CannotCreate(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_fail_{templateName}_{tfm.ToTFMString()}");

        var buildEnvToUse = tfm == TestTargetFramework.CurrentTFM ? BuildEnvironment.ForCurrentTFM : BuildEnvironment.ForPreviousTFM;
        var templateHive = tfm == TestTargetFramework.CurrentTFM ? TemplatesCustomHive.With9_0_Net8 : TemplatesCustomHive.With9_0_Net9;
        await templateHive.InstallAsync(buildEnvToUse);

        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id,
                templateName,
                _testOutput,
                buildEnvironment: buildEnvToUse,
                targetFramework: tfm,
                customHiveForTemplates: templateHive.CustomHiveDirectory);
        }
        catch (ToolCommandException tce)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains($"'{tfm.ToTFMString()}' is not a valid value for -f", tce.Result.Value.Output);
        }
    }
}
