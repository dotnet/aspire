// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Workload.Tests;

public class BuildEnvironment
{
    public string                           DotNet                        { get; init; }
    public bool                             IsWorkload                    { get; init; }
    public string                           DefaultBuildArgs              { get; init; }
    public IDictionary<string, string>      EnvVars                       { get; init; }
    public string                           LogRootPath                   { get; init; }

    public string                           WorkloadPacksDir              { get; init; }
    public string                           BuiltNuGetsPath               { get; init; }
    public bool                             HasSdkWithWorkload            { get; init; }
    public string                           TestAssetsPath                { get; init; }
    public string                           TestProjectPath               { get; init; }

    public static readonly string           TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");
    public static readonly string           TmpPath = Path.Combine(Path.GetTempPath(), "testroot");

    public static bool IsRunningOnHelix => Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static bool IsRunningOnCIBuildMachine => Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
    public static bool IsRunningOnCI => IsRunningOnHelix || IsRunningOnCIBuildMachine;

    public BuildEnvironment(bool expectSdkWithWorkload = true, Func<string, string, string>? sdkWithWorkloadNotFound = null)
    {
        DirectoryInfo? solutionRoot = new(AppContext.BaseDirectory);
        while (solutionRoot != null)
        {
            if (Directory.Exists(Path.Combine(solutionRoot.FullName, ".git")))
            {
                break;
            }

            solutionRoot = solutionRoot.Parent;
        }

        // HasSdkWithWorkload = EnvironmentVariables.TestsRunningOutOfTree || expectSdkWithWorkload;
        string sdkForWorkloadPath;
        if (solutionRoot is not null)
        {
            // Local run
            if (expectSdkWithWorkload)
            {
                var sdkDirName = string.IsNullOrEmpty(EnvironmentVariables.SdkDirName) ? "dotnet-latest" : EnvironmentVariables.SdkDirName;
                var probePath = Path.Combine(solutionRoot!.FullName, "artifacts", "bin", sdkDirName);
                if (Directory.Exists(probePath))
                {
                    sdkForWorkloadPath = Path.GetFullPath(probePath);
                }
                else
                {
                    string? prefix = sdkWithWorkloadNotFound?.Invoke(probePath, solutionRoot.FullName) ?? "";
                    throw new InvalidOperationException(
                        (prefix is not null ? $"{prefix}{Environment.NewLine}" : "") +
                        $"Could not find find a sdk with the workload installed at {probePath} computed from solutionRoot={solutionRoot}.{Environment.NewLine}" +
                        $"Build all the packages with '.\\build.cmd -pack'.{Environment.NewLine}" +
                        $"Then install the sdk+worklaod with 'dotnet build tests\\workloads.proj'");
                }
            }
            else
            {
                 string? dotnetPath = Environment.GetEnvironmentVariable("PATH")!
                    .Split(Path.PathSeparator)
                    .Select(path => Path.Combine(path, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet"))
                    .FirstOrDefault(File.Exists);
                if (dotnetPath is null)
                {
                    throw new ArgumentException($"Could not find dotnet.exe in PATH={Environment.GetEnvironmentVariable("PATH")}");
                }
                sdkForWorkloadPath = Path.GetDirectoryName(dotnetPath)!;
            }

            BuiltNuGetsPath = Path.Combine(solutionRoot.FullName, "artifacts", "packages", EnvironmentVariables.BuildConfiguration, "Shipping");

            // this is the only difference for local run but outside-of-repo
            if (expectSdkWithWorkload)
            {
                TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
            }
            else
            {
                TestAssetsPath = Path.Combine(solutionRoot!.FullName, "tests");
            }
            if (!Directory.Exists(TestAssetsPath))
            {
                throw new ArgumentException($"Cannot find TestAssetsPath={TestAssetsPath}");
            }
        }
        else
        {
            // CI
            if (string.IsNullOrEmpty(EnvironmentVariables.SdkForWorkloadTestingPath) || !Directory.Exists(EnvironmentVariables.SdkForWorkloadTestingPath))
            {
                throw new ArgumentException($"Cannot find 'SDK_FOR_WORKLOAD_TESTING_PATH={EnvironmentVariables.SdkForWorkloadTestingPath}'");
            }
            sdkForWorkloadPath = EnvironmentVariables.SdkForWorkloadTestingPath;

            if (string.IsNullOrEmpty(EnvironmentVariables.BuiltNuGetsPath) || !Directory.Exists(EnvironmentVariables.BuiltNuGetsPath))
            {
                throw new ArgumentException($"Cannot find 'BUILT_NUGETS_PATH={EnvironmentVariables.BuiltNuGetsPath}' or {BuiltNuGetsPath}");
            }
            BuiltNuGetsPath = EnvironmentVariables.BuiltNuGetsPath;

            TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
        }

        if (!string.IsNullOrEmpty(EnvironmentVariables.SdkForWorkloadTestingPath))
        {
            // always allow overridding the dotnet used for testing
            sdkForWorkloadPath = EnvironmentVariables.SdkForWorkloadTestingPath;
        }

        HasSdkWithWorkload = expectSdkWithWorkload;
        sdkForWorkloadPath = Path.GetFullPath(sdkForWorkloadPath);
        DefaultBuildArgs = string.Empty;
        WorkloadPacksDir = Path.Combine(sdkForWorkloadPath, "packs");
        TestProjectPath = Path.Combine(TestAssetsPath, "testproject");

        Console.WriteLine($"*** Using workload path: {sdkForWorkloadPath}");
        EnvVars = new Dictionary<string, string>();
        if (HasSdkWithWorkload)
        {
            EnvVars["DOTNET_ROOT"] = sdkForWorkloadPath;
            EnvVars["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
            EnvVars["BUILT_NUGETS_PATH"] = BuiltNuGetsPath;
            EnvVars["NUGET_PACKAGES"] = Path.Combine(BuildEnvironment.TmpPath, "nuget-cache");
        }

        DotNet = Path.Combine(sdkForWorkloadPath!, "dotnet");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DotNet += ".exe";
        }

        if (!string.IsNullOrEmpty(EnvironmentVariables.TestLogPath))
        {
            LogRootPath = Path.GetFullPath(EnvironmentVariables.TestLogPath);
            if (!Directory.Exists(LogRootPath))
            {
                Directory.CreateDirectory(LogRootPath);
            }
        }
        else
        {
            LogRootPath = Environment.CurrentDirectory;
        }

        if (Directory.Exists(TmpPath))
        {
            Directory.Delete(TmpPath, recursive: true);
        }

        Directory.CreateDirectory(TmpPath);
    }
}
