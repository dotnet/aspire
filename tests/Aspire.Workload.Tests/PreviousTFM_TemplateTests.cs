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

    /*

       Combinations:
        - Build for tfm
        - sdk to use
        - hive to use

            working: net9.0, hive: net9.0
            working: net8.0, hive: net8.0

            working: net9.0, hive: net9.0+net8.0
            working: net8.0, hive: net9.0+net8.0

            Also, working with both installed

            not working: tfm:net8.0
                    sdk: net8
                        hive: net9 - no -f?
                        hive: net8 - works
                    sdk: net9 - can't target 8?
                        hive: ~

            not working: tfm: net9.0
                    sdk: net8 - can't target 8?
                        hive: ~
                    sdk: net9
                        hive: net9 - works
                        hive: net8 - no -f n9
    */

    [Theory]
    [InlineData("aspire", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire", TestTargetFramework.PreviousTFM)]
    [InlineData("aspire-starter", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire-starter", TestTargetFramework.PreviousTFM)]
    public async Task CanNewAndBuildWithMatchingSdkAndTemplate(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");
        var (buildEnvToUse, templateHive) = tfm switch
        {
            TestTargetFramework.CurrentTFM => (BuildEnvironment.ForCurrentSdk, TemplatesCustomHive.With9_0_Net9),
            TestTargetFramework.PreviousTFM => (BuildEnvironment.ForPreviousSdk, TemplatesCustomHive.With9_0_Net8),
            _ => throw new ArgumentOutOfRangeException(nameof(tfm))
        };

        await templateHive.InstallAsync(buildEnvToUse);
        await using var project = await AspireProject.CreateNewTemplateProjectAsync(
            id,
            templateName,
            _testOutput,
            buildEnvironment: buildEnvToUse,
            targetFramework: tfm,
            customHiveForTemplates: templateHive.CustomHiveDirectory);

        await project.BuildAsync(extraBuildArgs: [$"-c Debug"]);
    }

    // FIXME: tests for other templates like tests

    [Theory]
    [InlineData("aspire", TestTargetFramework.CurrentTFM)]
    [InlineData("aspire", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-starter", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-starter", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-apphost", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-apphost", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-servicedefaults", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-servicedefaults", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-mstest", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-mstest", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-xunit", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-xunit", TestTargetFramework.PreviousTFM)]
    // [InlineData("aspire-nunit", TestTargetFramework.CurrentTFM)]
    // [InlineData("aspire-nunit", TestTargetFramework.PreviousTFM)]
    public async Task CannotNewWithMismatchedSdkAndTemplate(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_fail_{templateName}_{tfm.ToTFMString()}");
        var (buildEnvToUse, templateHive) = tfm switch
        {
            TestTargetFramework.CurrentTFM => (BuildEnvironment.ForPreviousSdk, TemplatesCustomHive.With9_0_Net9),
            TestTargetFramework.PreviousTFM => (BuildEnvironment.ForCurrentSdk, TemplatesCustomHive.With9_0_Net8),
            _ => throw new ArgumentOutOfRangeException(nameof(tfm))
        };

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
