// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Workload.Tests;

public class BuildEnvironment
{
    public string                           DotNet                        { get; init; }
    public string                           DefaultBuildArgs              { get; init; }
    public IDictionary<string, string>      EnvVars                       { get; init; }
    public string                           LogRootPath                   { get; init; }

    public string                           WorkloadPacksDir              { get; init; }
    public string                           BuiltNuGetsPath               { get; init; }
    public bool                             HasWorkloadFromArtifacts      { get; init; }
    public string                           TestAssetsPath                { get; set; }
    public string                           NuGetPackagesPath             { get; init; }
    public TestTargetFramework              TargetFramework               { get; init; }
    public DirectoryInfo?                   RepoRoot                      { get; init; }

    public const TestTargetFramework        DefaultTargetFramework = TestTargetFramework.Net80;
    public static readonly string           TestDataPath = Path.Combine(AppContext.BaseDirectory, "data");
    public static readonly string           TestRootPath = Path.Combine(Path.GetTempPath(), "testroot");

    public static bool IsRunningOnHelix => Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static bool IsRunningOnCIBuildMachine => Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
    public static bool IsRunningOnCI => IsRunningOnHelix || IsRunningOnCIBuildMachine;

    private static readonly Lazy<BuildEnvironment> s_instance_80 = new(() => new BuildEnvironment(targetFramework: TestTargetFramework.Net80));

    public static BuildEnvironment ForNet80 => s_instance_80.Value;
    public static BuildEnvironment ForDefaultFramework => ForNet80;

    public BuildEnvironment(bool useSystemDotNet = true, TestTargetFramework targetFramework = DefaultTargetFramework)
    {
        TargetFramework = targetFramework;
        RepoRoot = new(AppContext.BaseDirectory);
        while (RepoRoot != null)
        {
            // To support git worktrees, check for either a directory or a file named ".git"
            if (Directory.Exists(Path.Combine(RepoRoot.FullName, ".git")) || File.Exists(Path.Combine(RepoRoot.FullName, ".git")))
            {
                break;
            }

            RepoRoot = RepoRoot.Parent;
        }

        string sdkForWorkloadPath;
        if (RepoRoot is not null)
        {
            // Local run
            if (!useSystemDotNet)
            {
                var sdkDirName = string.IsNullOrEmpty(EnvironmentVariables.SdkDirName) ? "dotnet-latest" : EnvironmentVariables.SdkDirName;
                var sdkFromArtifactsPath = Path.Combine(RepoRoot!.FullName, "artifacts", "bin", sdkDirName);
                if (Directory.Exists(sdkFromArtifactsPath))
                {
                    sdkForWorkloadPath = Path.GetFullPath(sdkFromArtifactsPath);
                }
                else
                {
                    string buildCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".\\build.cmd" : "./build.sh";
                    string workloadsProjString = Path.Combine("tests", "workloads.proj");
                    throw new InvalidOperationException(
                        $"Could not find a sdk with the workload installed at {sdkFromArtifactsPath} computed from solutionRoot={RepoRoot}.{Environment.NewLine}" +
                        $"Build all the packages with '{buildCmd} -pack'.{Environment.NewLine}" +
                        $"Then install the sdk+workload with 'dotnet build {workloadsProjString}'");
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

            BuiltNuGetsPath = Path.Combine(RepoRoot.FullName, "artifacts", "packages", EnvironmentVariables.BuildConfiguration, "Shipping");
            TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
            if (!Directory.Exists(TestAssetsPath))
            {
                throw new ArgumentException($"Cannot find TestAssetsPath={TestAssetsPath}");
            }
        }
        else
        {
            // CI - helix
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

        HasWorkloadFromArtifacts = !useSystemDotNet;
        sdkForWorkloadPath = Path.GetFullPath(sdkForWorkloadPath);
        DefaultBuildArgs = string.Empty;
        WorkloadPacksDir = Path.Combine(sdkForWorkloadPath, "packs");
        NuGetPackagesPath = Path.Combine(AppContext.BaseDirectory, $"nuget-cache-{TargetFramework}");

        EnvVars = new Dictionary<string, string>();
        if (HasWorkloadFromArtifacts)
        {
            EnvVars["DOTNET_ROOT"] = sdkForWorkloadPath;
            EnvVars["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
            EnvVars["BUILT_NUGETS_PATH"] = BuiltNuGetsPath;
            EnvVars["NUGET_PACKAGES"] = NuGetPackagesPath;
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
            LogRootPath = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        if (Directory.Exists(TestRootPath))
        {
            Directory.Delete(TestRootPath, recursive: true);
        }

        Directory.CreateDirectory(TestRootPath);

        Console.WriteLine($"*** [{TargetFramework}] Using workload path: {sdkForWorkloadPath}");
        Console.WriteLine($"*** [{TargetFramework}] Using NuGet cache (never deleted automatically): {NuGetPackagesPath}");
        Console.WriteLine($"*** [{TargetFramework}] Using path for projects: {TestRootPath}");
    }
}

public enum TestTargetFramework
{
    Net80,
    Net90
}
