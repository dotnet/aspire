// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.EndToEnd.Tests;

public class BuildEnvironment
{
    public string                           DotNet                        { get; init; }
    public bool                             IsWorkload                    { get; init; }
    public string                           DefaultBuildArgs              { get; init; }
    public IDictionary<string, string>      EnvVars                       { get; init; }
    public string                           LogRootPath                   { get; init; }

    public string                           WorkloadPacksDir              { get; init; }
    public string                           BuiltNuGetsPath               { get; init; }

    public static bool IsRunningOnHelix => Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static bool IsRunningOnCIBuildMachine => Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
    public static bool IsRunningOnCI => IsRunningOnHelix || IsRunningOnCIBuildMachine;

    public string           TestAssetsPath { get; init; }
    public string TestProjectPath { get; init; }
    public static readonly string           TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");
    public static readonly string           TmpPath = Path.Combine(Path.GetTempPath(), "testroot");

    public BuildEnvironment(bool forceOutOfTree = false)
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

        string sdkForWorkloadPath;
        if (solutionRoot is not null)
        {
            if (EnvironmentVariables.TestsRunningOutOfTree || forceOutOfTree)
            {
                // Is this a "local run?
                var sdkDirName = string.IsNullOrEmpty(EnvironmentVariables.SdkDirName) ? "dotnet-latest" : EnvironmentVariables.SdkDirName;
                var probePath = Path.Combine(solutionRoot!.FullName, "artifacts", "bin", sdkDirName);
                if (Directory.Exists(probePath))
                {
                    sdkForWorkloadPath = Path.GetFullPath(probePath);
                }
                else
                {
                    throw new ArgumentException($"Running out-of-tree: Could not find {probePath} computed from solutionRoot={solutionRoot}. Build all the packages with `./build -pack`. And install the sdk+workload 'dotnet tests/Aspire.EndToEnd.Tests/Aspire.EndToEnd.csproj /t:InstallWorkloadUsingArtifacts /p:Configuration=<config>");
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

            // this is the only difference for local run but out-of-tree
            if (EnvironmentVariables.TestsRunningOutOfTree)
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
            // FIXME: extra check empty/exists to a func
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

        sdkForWorkloadPath = Path.GetFullPath(sdkForWorkloadPath);
        DefaultBuildArgs = string.Empty;
        WorkloadPacksDir = Path.Combine(sdkForWorkloadPath, "packs");
        TestProjectPath = Path.Combine(TestAssetsPath, "testproject");

        Console.WriteLine ($"*** Using workload path: {sdkForWorkloadPath}");
        EnvVars = new Dictionary<string, string>
        {
            ["DOTNET_ROOT"] = sdkForWorkloadPath,
            ["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath,
            ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
            ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
            ["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}",
            ["BUILT_NUGETS_PATH"] = BuiltNuGetsPath
        };

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
