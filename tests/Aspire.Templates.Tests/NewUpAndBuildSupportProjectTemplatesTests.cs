// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Templates.Tests;

public abstract class NewUpAndBuildSupportProjectTemplatesBase(ITestOutputHelper testOutput) : TemplateTestsBase(testOutput)
{
    public const string AspireVersionNext = "13.0";

    [Trait("category", "basic-build")]
    protected async Task CanNewAndBuildActual(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        var id = GetNewProjectId(prefix: $"new_build_{FixupSymbolName(templateName)}");
        var topLevelDir = Path.Combine(BuildEnvironment.TestRootPath, id + "_root");
        string config = "Debug";

        var buildEnvToUse = sdk switch
        {
            TestSdk.Current => BuildEnvironment.ForCurrentSdkOnly,
            TestSdk.Previous => BuildEnvironment.ForPreviousSdkOnly,
            TestSdk.Next => BuildEnvironment.ForNextSdkOnly,
            TestSdk.CurrentSdkAndPreviousRuntime => BuildEnvironment.ForCurrentSdkAndPreviousRuntime,
            TestSdk.NextSdkAndCurrentRuntime => BuildEnvironment.ForNextSdkAndCurrentRuntime,
            _ => throw new ArgumentOutOfRangeException(nameof(sdk))
        };

        if (Directory.Exists(topLevelDir))
        {
            Directory.Delete(topLevelDir, recursive: true);
        }
        Directory.CreateDirectory(topLevelDir);

        try
        {
            await using var project = await AspireProject.CreateNewTemplateProjectAsync(
                id: id + ".AppHost",
                template: "aspire-apphost",
                testOutput: _testOutput,
                buildEnvironment: buildEnvToUse,
                targetFramework: tfm,
                addEndpointsHook: false,
                overrideRootDir: topLevelDir);
            project.AppHostProjectDirectory = Path.Combine(topLevelDir, id + ".AppHost");

            var testProjectDir = await CreateAndAddTestTemplateProjectAsync(
                                        id: id,
                                        testTemplateName: templateName,
                                        project: project,
                                        tfm: tfm,
                                        buildEnvironment: buildEnvToUse,
                                        extraArgs: extraTestCreationArgs,
                                        overrideRootDir: topLevelDir);

            await project.BuildAsync(extraBuildArgs: [$"-c {config}"], workingDirectory: testProjectDir);
        }
        catch (ToolCommandException tce) when (error is not null)
        {
            Assert.NotNull(tce.Result);
            Assert.Contains(error, tce.Result.Value.Output);
        }
    }
}

public class NUnit_AspireVersionCurrent_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-nunit", ""])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class NUnit_AspireVersionNext_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-nunit", $"--aspire-version {AspireVersionNext}"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_Default_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", ""])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_V2_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", "--xunit-version v2"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_V3_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", "--xunit-version v3"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_V3MTP_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", "--xunit-version v3mtp"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_AspireVersion_Current_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", ""])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class XUnit_AspireVersion_Next_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-xunit", $"--aspire-version {AspireVersionNext}"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class MSTest_AspireVersionCurrent_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-mstest", ""])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class MSTest_AspireVersionNext_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["aspire-mstest", $"--aspire-version {AspireVersionNext}"])]
    public Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        return CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}

public class MauiServiceDefaults_AspireVersionNext_NewUpAndBuildSupportProjectTemplatesTests(ITestOutputHelper testOutput) : NewUpAndBuildSupportProjectTemplatesBase(testOutput)
{
    [Theory]
    [MemberData(nameof(TestDataForNewAndBuildTemplateTests), arguments: ["maui-aspire-servicedefaults", $"--aspire-version {AspireVersionNext}"])]
    public async Task CanNewAndBuild(string templateName, string extraTestCreationArgs, TestSdk sdk, TestTargetFramework tfm, string? error)
    {
        // MAUI templates require .NET 10.0 or later. Skip tests with earlier target frameworks,
        // unless an error is expected (which means we're testing that the SDK correctly rejects it).
        var tfmString = tfm.ToTFMString();
        if ((tfmString == "net8.0" || tfmString == "net9.0") && string.IsNullOrEmpty(error))
        {
            Assert.Skip($"MAUI templates require .NET 10.0 or later, skipping TFM {tfmString}");
        }

        await CanNewAndBuildActual(templateName, extraTestCreationArgs, sdk, tfm, error);
    }
}
