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

            1. Also, working with both installed


            - CNAB_WithMatchingSdkAndTemplate
                working: net8.0, sdk: 8, hive: net8.0
                working: net9.0, sdk: 8, hive: net9.0
            - CNAB_WithMatchingSdkAndMismatchedTemplates
                not working: net8.0, sdk: 8, hive: net9.0
                not working: net9.0, sdk: 8, hive: net8.0
            - CNAB_

            2. TFM: net8
                working: net8.0, hive: net9.0+net8.0

                not working: tfm:net8.0
                        sdk: net8
                            hive: net9 - no -f?
                            hive: net8 - works
                        sdk: net9 - can't target 8?
                            hive: ~
            3. TFM: net9
                working: net9.0, hive: net9.0+net8.0

                not working: tfm: net9.0
                        sdk: net8 - can't target 8?
                            hive: ~
                        sdk: net9
                            hive: net9 - works
                            hive: net8 - no -f n9
    */

    public static TheoryData<TestTargetFramework, TestSdk, TestTemplatesInstall, string?> TestData() => new()
        {
            // Previous TFM
            { TestTargetFramework.Previous, TestSdk.Previous, TestTemplatesInstall.Net8, null},
            { TestTargetFramework.Previous, TestSdk.Previous, TestTemplatesInstall.Net9, "'net8.0' is not a valid value for -f"},
            { TestTargetFramework.Previous, TestSdk.Previous, TestTemplatesInstall.Net9AndNet8, null},

            { TestTargetFramework.Previous, TestSdk.Current, TestTemplatesInstall.Net8, null},
            { TestTargetFramework.Previous, TestSdk.Current, TestTemplatesInstall.Net9, "'net8.0' is not a valid value for -f"},
            { TestTargetFramework.Previous, TestSdk.Current, TestTemplatesInstall.Net9AndNet8, null},

            // Current TFM
            { TestTargetFramework.Current, TestSdk.Previous, TestTemplatesInstall.Net8, "'net9.0' is not a valid value for -f"},
            { TestTargetFramework.Current, TestSdk.Previous, TestTemplatesInstall.Net9, "The current .NET SDK does not support targeting .NET 9.0"},
            { TestTargetFramework.Current, TestSdk.Previous, TestTemplatesInstall.Net9AndNet8, "The current .NET SDK does not support targeting .NET 9.0"},

            { TestTargetFramework.Current, TestSdk.Current, TestTemplatesInstall.Net8, "'net9.0' is not a valid value for -f"},
            { TestTargetFramework.Current, TestSdk.Current, TestTemplatesInstall.Net9, null},
            { TestTargetFramework.Current, TestSdk.Current, TestTemplatesInstall.Net9AndNet8, null},
        };

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task NewAndBuildTemplate(TestTargetFramework tfm, TestSdk sdk, TestTemplatesInstall templates, string? error)
    {
        _ = error;
        string templateName = "aspire";
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdk,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdk,
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

    [Theory]
    [InlineData("aspire", TestTargetFramework.Previous)]
    [InlineData("aspire-starter", TestTargetFramework.Previous)]
    public async Task CanNewAndBuildWithMatchingSdkAndTemplate(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");
        var (buildEnvToUse, templateHive) = tfm switch
        {
            TestTargetFramework.Current => (BuildEnvironment.ForCurrentSdk, TemplatesCustomHive.With9_0_Net9),
            TestTargetFramework.Previous => (BuildEnvironment.ForPreviousSdk, TemplatesCustomHive.With9_0_Net8),
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

    [Theory]
    [InlineData("aspire", TestTargetFramework.Previous)]
    [InlineData("aspire-starter", TestTargetFramework.Previous)]
    public async Task CanNewAndBuildWithMatchingSdkAndBothTemplates(string templateName, TestTargetFramework tfm)
    {
        var id = GetNewProjectId(prefix: $"new_build_{templateName}_{tfm.ToTFMString()}");
        var (buildEnvToUse, templateHive) = tfm switch
        {
            TestTargetFramework.Current => (BuildEnvironment.ForCurrentSdk, TemplatesCustomHive.With9_0_Net9_And_Net8),
            TestTargetFramework.Previous => (BuildEnvironment.ForPreviousSdk, TemplatesCustomHive.With9_0_Net9_And_Net8),
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

    public enum TestSdk
    {
        Previous,
        Current
    }

    public enum TestTemplatesInstall
    {
        Net9,
        Net8,
        Net9AndNet8
    }
}
