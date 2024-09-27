// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class NewAndBuildStandaloneTemplateTests(ITestOutputHelper testOutput) : WorkloadTestsBase(testOutput)
{
    public static TheoryData<string, TestSdk, TestTargetFramework, TestTemplatesInstall, string?> TestData(string templateName) => new()
        {
            // Previous Sdk
            { templateName, TestSdk.Previous, TestTargetFramework.Previous, TestTemplatesInstall.Net8, null },
            { templateName, TestSdk.Previous, TestTargetFramework.Previous, TestTemplatesInstall.Net9, "'net8.0' is not a valid value for -f" },
            { templateName, TestSdk.Previous, TestTargetFramework.Previous, TestTemplatesInstall.Net9AndNet8, null },

            // { templateName, TestSdk.Previous, TestTargetFramework.Current, TestTemplatesInstall.Net8, "'net9.0' is not a valid value for -f" },
            // { templateName, TestSdk.Previous, TestTargetFramework.Current, TestTemplatesInstall.Net9, "The current .NET SDK does not support targeting .NET 9.0" },
            // { templateName, TestSdk.Previous, TestTargetFramework.Current, TestTemplatesInstall.Net9AndNet8, "The current .NET SDK does not support targeting .NET 9.0" },

            // // Current SDK
            // { templateName, TestSdk.Current, TestTargetFramework.Previous, TestTemplatesInstall.Net8, null },
            // { templateName, TestSdk.Current, TestTargetFramework.Previous, TestTemplatesInstall.Net9, "'net8.0' is not a valid value for -f" },
            // { templateName, TestSdk.Current, TestTargetFramework.Previous, TestTemplatesInstall.Net9AndNet8, null },

            // { templateName, TestSdk.Current, TestTargetFramework.Current, TestTemplatesInstall.Net8, "'net9.0' is not a valid value for -f" },
            // { templateName, TestSdk.Current, TestTargetFramework.Current, TestTemplatesInstall.Net9, null },
            // { templateName, TestSdk.Current, TestTargetFramework.Current, TestTemplatesInstall.Net9AndNet8, null },

            // // Current SDK + previous runtime
            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Previous, TestTemplatesInstall.Net8, null },
            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Previous, TestTemplatesInstall.Net9, "'net8.0' is not a valid value for -f" },
            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Previous, TestTemplatesInstall.Net9AndNet8, null },

            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Current, TestTemplatesInstall.Net8, "'net9.0' is not a valid value for -f" },
            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Current, TestTemplatesInstall.Net9, null },
            // { templateName, TestSdk.CurrentSdkAndPreviousRuntime, TestTargetFramework.Current, TestTemplatesInstall.Net9AndNet8, null },
        };

    [Theory]
    [MemberData(nameof(TestData), parameters: "aspire")]
    [MemberData(nameof(TestData), parameters: "aspire-starter")]
    public async Task CanNewAndBuild(string templateName, TestSdk sdk, TestTargetFramework tfm, TestTemplatesInstall templates, string? error)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdk,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdk,
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

            Assert.True(error is null, $"Expected to throw an exception with message: {error}");

            await project.BuildAsync(extraBuildArgs: [$"-c Debug"]);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }

    // FIXME: Rename. move to a separate class?
    [Theory]
    // [MemberData(nameof(TestData), parameters: "aspire-apphost")]
    // [MemberData(nameof(TestData), parameters: "aspire-servicedefaults")]
    [MemberData(nameof(TestData), parameters: "aspire-mstest")]
    [MemberData(nameof(TestData), parameters: "aspire-nunit")]
    [MemberData(nameof(TestData), parameters: "aspire-xunit")]
    public async Task CanNewAndBuildOthers(string templateName, TestSdk sdk, TestTargetFramework tfm, TestTemplatesInstall templates, string? error)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");
        string config = "Debug";

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdk,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdk,
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

        await templateHive.InstallAsync(buildEnvToUse);
        try
        {
            var (project, testProjectDir) = await CreateFromAspireTemplateWithTestAsync(id, config, templateName, tfm, buildEnvToUse, templateHive);

            await project.BuildAsync(extraBuildArgs: [$"-c {config}"], workingDirectory: testProjectDir);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }

    // FIXME: tests for other templates like tests

    [Theory]
    [InlineData("aspire", TestTargetFramework.Current)]
    [InlineData("aspire", TestTargetFramework.Previous)]
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
            TestTargetFramework.Current => (BuildEnvironment.ForPreviousSdk, TemplatesCustomHive.With9_0_Net9),
            TestTargetFramework.Previous => (BuildEnvironment.ForCurrentSdk, TemplatesCustomHive.With9_0_Net8),
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
