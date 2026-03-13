// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;

namespace Aspire.Templates.Tests;

public static class EnvironmentVariables
{
    private const string TemplateTestProjectPath = "tests/Aspire.Templates.Tests/Aspire.Templates.Tests.csproj";

    public static readonly string? SdkForTemplateTestingPath       = Environment.GetEnvironmentVariable("SDK_FOR_TEMPLATES_TESTING_PATH");
    public static readonly string? TestLogPath                     = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
    public static readonly string? SkipProjectCleanup              = Environment.GetEnvironmentVariable("SKIP_PROJECT_CLEANUP");
    public static readonly string? BuiltNuGetsPath                 = Environment.GetEnvironmentVariable("BUILT_NUGETS_PATH");
    public static readonly bool    ShowBuildOutput                 = Environment.GetEnvironmentVariable("SHOW_BUILD_OUTPUT") is "true";
    public static readonly bool    IsRunningOnCI                   = PlatformDetection.IsRunningOnCI;
    public static readonly bool    TestsRunningOutsideOfRepo       = Environment.GetEnvironmentVariable("TestsRunningOutsideOfRepo") is "true";
    public static readonly string? TestScenario                    = Environment.GetEnvironmentVariable("TEST_SCENARIO");
    public static readonly string? DefaultTFMForTesting            = Environment.GetEnvironmentVariable("DEFAULT_TFM_FOR_TESTING");
    public static readonly string? TestRootPath                    = Environment.GetEnvironmentVariable("DEV_TEMP");
    public static readonly bool    ConditionalSelectionRunAll      = Environment.GetEnvironmentVariable("ConditionalSelectionRunAll") is "true";
    public static readonly string? DirectlyAffectedTestProjects    = Environment.GetEnvironmentVariable("DirectlyAffectedTestProjects");
    public static readonly bool    IsDirectlyAffectedProject       = IsDirectlyAffected(TemplateTestProjectPath, DirectlyAffectedTestProjects);
    public static readonly bool    RunOnlyBasicBuildTemplatesTests = Environment.GetEnvironmentVariable("RunOnlyBasicBuildTemplateTests") is "true"
        || (!ConditionalSelectionRunAll
            && !string.IsNullOrWhiteSpace(DirectlyAffectedTestProjects)
            && !IsDirectlyAffectedProject);

    private static bool IsDirectlyAffected(string projectPath, string? directlyAffectedProjects)
    {
        if (string.IsNullOrWhiteSpace(directlyAffectedProjects))
        {
            return false;
        }

        var normalizedProjectPath = $";{projectPath.Replace('\\', '/')};";
        var normalizedDirectlyAffectedProjects = $";{directlyAffectedProjects.Replace('\\', '/')};";

        return normalizedDirectlyAffectedProjects.Contains(normalizedProjectPath, StringComparison.OrdinalIgnoreCase);
    }
}
