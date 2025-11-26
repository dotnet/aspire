// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Templates.Tests;

public class NewUpAndBuildServiceDefaultsTemplatesTests(ITestOutputHelper testOutput) : TemplateTestsBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-servicedefaults", "--aspire-version 13.0"])]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-maui-servicedefaults", "--aspire-version 13.0"])]
    [Trait("category", "basic-build")]
    public async Task CanNewAndBuild(string templateName, string extraArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        // MAUI templates require .NET 10.0 or later; skip older TFMs unless an error is expected.
        if (templateName == "aspire-maui-servicedefaults")
        {
            var tfmString = tfm.ToTFMString();
            if ((tfmString == "net8.0" || tfmString == "net9.0") && string.IsNullOrEmpty(error))
            {
                Assert.Skip($"MAUI templates require .NET 10.0 or later, skipping TFM {tfmString}");
            }
        }

        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdkOnly,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdkOnly,
            TestSdk.Next => BuildEnvironment.ForNextSdkOnly,
            TestSdk.CurrentSdkAndPreviousRuntime => BuildEnvironment.ForCurrentSdkAndPreviousRuntime,
            TestSdk.NextSdkAndCurrentRuntime => BuildEnvironment.ForNextSdkAndCurrentRuntime,
            _ => throw new ArgumentOutOfRangeException(nameof(sdk))
        };

        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id,
                templateName,
                _testOutput,
                buildEnvironment: buildEnvToUse,
                extraArgs: extraArgs,
                targetFramework: tfm,
                addEndpointsHook: false);

            Assert.True(error is null, $"Expected to throw an exception with message: {error}");

                await project.BuildAsync(extraBuildArgs: ["-c Debug"], workingDirectory: project.RootDir);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }
}
