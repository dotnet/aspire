// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class NewAndBuildStandaloneTemplateTests(ITestOutputHelper testOutput) : WorkloadTestsBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), parameters: "aspire")]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), parameters: "aspire-starter")]
    public async Task CanNewAndBuild(string templateName, TestSdk sdk, TestTargetFramework tfm, TestTemplatesInstall templates, string? error)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdkOnly,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdkOnly,
            TestSdk.CurrentSdkAndPreviousRuntime => BuildEnvironment.ForCurrentSdkAndPreviousRuntime,
            _ => throw new ArgumentOutOfRangeException(nameof(sdk))
        };

        var templateHive = templates switch
        {
            TestTemplatesInstall.Net8 => TemplatesCustomHive.With9_0_Net8,
            TestTemplatesInstall.Net9 => TemplatesCustomHive.With9_0_Net9,
            TestTemplatesInstall.Net9AndNet8 => TemplatesCustomHive.With9_0_Net9_And_Net8,
            _ => throw new ArgumentOutOfRangeException(nameof(templates))
        };

        await templateHive.EnsureInstalledAsync(buildEnvToUse);
        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id,
                templateName,
                _testOutput,
                buildEnvironment: buildEnvToUse,
                targetFramework: tfm,
                customHiveForTemplates: templateHive.CustomHiveDirectory);

            Assert.True(error is null, $"Expected to throw an exception with message: {error}");

            await project.BuildAsync(extraBuildArgs: [$"-c Debug"]);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }
}
