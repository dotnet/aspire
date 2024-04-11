// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Workload.Tests;

public static class EnvironmentVariables
{
    public static readonly string? SdkForWorkloadTestingPath = Environment.GetEnvironmentVariable("SDK_FOR_WORKLOAD_TESTING_PATH");
    public static readonly string? TestLogPath               = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
    public static readonly string? SkipProjectCleanup        = Environment.GetEnvironmentVariable("SKIP_PROJECT_CLEANUP");
    public static readonly string? BuiltNuGetsPath           = Environment.GetEnvironmentVariable("BUILT_NUGETS_PATH");
    public static readonly bool    ShowBuildOutput           = Environment.GetEnvironmentVariable("SHOW_BUILD_OUTPUT") is "true";
    public static readonly string? SdkDirName                = Environment.GetEnvironmentVariable("SDK_DIR_NAME");
    public static readonly bool    IsRunningOnCI             = Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static readonly bool    TestsRunningOutsideOfRepo     = Environment.GetEnvironmentVariable("TestsRunningOutsideOfRepo") is "true";
    public static readonly string  BuildConfiguration        = Environment.GetEnvironmentVariable("BUILD_CONFIGURATION") ?? "Debug";
    public static readonly string? TestAssetsPath            = Environment.GetEnvironmentVariable("TEST_ASSETS_PATH");
    public static readonly string? TestScenario              = Environment.GetEnvironmentVariable("TEST_SCENARIO");
}
