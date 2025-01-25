// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Diagnostics.Latency;
using Xunit.Sdk;

namespace Aspire.Workload.Tests;

public class BuildEnvironment
{
    public string                           DotNet                        { get; init; }
    public string                           DefaultBuildArgs              { get; init; }
    public IDictionary<string, string>      EnvVars                       { get; init; }
    public string                           LogRootPath                   { get; init; }

    public string                           BuiltNuGetsPath               { get; init; }
    public bool                             UsesCustomDotNet              { get; init; }
    public bool                             UsesSystemDotNet => !UsesCustomDotNet;
    public string?                          NuGetPackagesPath             { get; init; }
    public DirectoryInfo?                   RepoRoot                      { get; init; }
    public TemplatesCustomHive?             TemplatesCustomHive           { get; init; }

    public static readonly string TempDir = IsRunningOnCI
        ? Path.GetTempPath()
        : Environment.GetEnvironmentVariable("DEV_TEMP") is { } devTemp && Path.Exists(devTemp)
            ? devTemp
            : Path.GetTempPath();

    public static readonly TestTargetFramework DefaultTargetFramework = ComputeDefaultTargetFramework();
    public static readonly string           TestAssetsPath = Path.Combine(AppContext.BaseDirectory, "testassets");
    public static readonly string           TestRootPath = Path.Combine(TempDir, "templates-testroot");

    public static bool IsRunningOnHelix => Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT") is not null;
    public static bool IsRunningOnCIBuildMachine => Environment.GetEnvironmentVariable("BUILD_BUILDID") is not null;
    public static bool IsRunningOnCI => IsRunningOnHelix || IsRunningOnCIBuildMachine;

    private static readonly Lazy<BuildEnvironment> s_instance_80 = new(() =>
        new BuildEnvironment(
            templatesCustomHive: TemplatesCustomHive.TemplatesHive,
            sdkDirName: "dotnet-8"));

    private static readonly Lazy<BuildEnvironment> s_instance_90 = new(() =>
        new BuildEnvironment(
            templatesCustomHive: TemplatesCustomHive.TemplatesHive,
            sdkDirName: "dotnet-9"));

    private static readonly Lazy<BuildEnvironment> s_instance_90_80 = new(() =>
        new BuildEnvironment(
            templatesCustomHive: TemplatesCustomHive.TemplatesHive,
            sdkDirName: "dotnet-tests"));

    public static BuildEnvironment ForPreviousSdkOnly => s_instance_80.Value;
    public static BuildEnvironment ForCurrentSdkOnly => s_instance_90.Value;
    public static BuildEnvironment ForCurrentSdkAndPreviousRuntime => s_instance_90_80.Value;

    public static BuildEnvironment ForDefaultFramework =>
        DefaultTargetFramework switch
        {
            TestTargetFramework.Previous => ForPreviousSdkOnly,

            // Use current+previous to allow running tests on helix built with 9.0 sdk
            // but targeting 8.0 tfm
            TestTargetFramework.Current => ForCurrentSdkAndPreviousRuntime,

            _ => throw new ArgumentOutOfRangeException(nameof(DefaultTargetFramework))
        };

    public BuildEnvironment(bool useSystemDotNet = false, TemplatesCustomHive? templatesCustomHive = default, string sdkDirName = "dotnet-tests")
    {
        UsesCustomDotNet = !useSystemDotNet;
        RepoRoot = TestUtils.FindRepoRoot();

        string sdkForWorkloadPath;
        if (RepoRoot is not null)
        {
            // Local run
            if (!useSystemDotNet)
            {
                var sdkFromArtifactsPath = Path.Combine(RepoRoot!.FullName, "artifacts", "bin", sdkDirName);
                if (Directory.Exists(sdkFromArtifactsPath))
                {
                    sdkForWorkloadPath = Path.GetFullPath(sdkFromArtifactsPath);
                }
                else
                {
                    string buildCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".\\build.cmd" : "./build.sh";
                    string workloadsProjString = Path.Combine("tests", "workloads.proj");
                    throw new XunitException(
                        $"Could not find a sdk with the workload installed at {sdkFromArtifactsPath} computed from {nameof(RepoRoot)}={RepoRoot}." +
                        $" Build all the packages with '{buildCmd} -pack'." +
                        $" Then install the sdk+workload with 'dotnet build {workloadsProjString}'." +
                        " See https://github.com/dotnet/aspire/tree/main/tests/Aspire.Workload.Tests#readme for more details.");
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

            PlaywrightProvider.DetectAndSetInstalledPlaywrightDependenciesPath(RepoRoot);
        }
        else
        {
            // CI - helix
            if (string.IsNullOrEmpty(EnvironmentVariables.SdkForWorkloadTestingPath))
            {
                throw new ArgumentException($"Environment variable SDK_FOR_WORKLOAD_TESTING_PATH is unset");
            }

            string? baseDir = Path.GetDirectoryName(EnvironmentVariables.SdkForWorkloadTestingPath);
            if (baseDir is null)
            {
                throw new ArgumentException($"Cannot find base directory for SDK_FOR_WORKLOAD_TESTING_PATH - {baseDir}");
            }

            sdkForWorkloadPath = Path.Combine(baseDir, sdkDirName);

            if (string.IsNullOrEmpty(EnvironmentVariables.BuiltNuGetsPath) || !Directory.Exists(EnvironmentVariables.BuiltNuGetsPath))
            {
                throw new ArgumentException($"Cannot find 'BUILT_NUGETS_PATH={EnvironmentVariables.BuiltNuGetsPath}' or {BuiltNuGetsPath}");
            }
            BuiltNuGetsPath = EnvironmentVariables.BuiltNuGetsPath;
        }

        if (!Directory.Exists(TestAssetsPath))
        {
            throw new ArgumentException($"Cannot find TestAssetsPath={TestAssetsPath}");
        }

        sdkForWorkloadPath = Path.GetFullPath(sdkForWorkloadPath);
        DefaultBuildArgs = string.Empty;
        NuGetPackagesPath = UsesCustomDotNet ? Path.Combine(AppContext.BaseDirectory, $"nuget-cache-{Guid.NewGuid()}") : null;
        EnvVars = new Dictionary<string, string>();
        if (UsesCustomDotNet)
        {
            EnvVars["DOTNET_ROOT"] = sdkForWorkloadPath;
            EnvVars["DOTNET_INSTALL_DIR"] = sdkForWorkloadPath;
            EnvVars["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            EnvVars["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
            EnvVars["PATH"] = $"{sdkForWorkloadPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";
        }
        EnvVars["NUGET_PACKAGES"] = NuGetPackagesPath!;
        EnvVars["BUILT_NUGETS_PATH"] = BuiltNuGetsPath;
        EnvVars["TreatWarningsAsErrors"] = "true";
        // Set DEBUG_SESSION_PORT='' to avoid the app from the tests connecting
        // to the IDE
        EnvVars["DEBUG_SESSION_PORT"] = "";
        // Avoid using the msbuild terminal logger, so the output can be read
        // in the tests
        EnvVars["_MSBUILDTLENABLED"] = "0";
        // .. and disable new output style for vstest
        EnvVars["VsTestUseMSBuildOutput"] = "false";
        EnvVars["SkipAspireWorkloadManifest"] = "true";

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

        Console.WriteLine($"*** Using path for projects: {TestRootPath}");
        CleanupTestRootPath();
        Directory.CreateDirectory(TestRootPath);

        Console.WriteLine($"*** Using Sdk path: {sdkForWorkloadPath}");
        if (UsesCustomDotNet)
        {
            if (EnvironmentVariables.IsRunningOnCI)
            {
                Console.WriteLine($"*** Using NuGet cache: {NuGetPackagesPath}");
                if (Directory.Exists(NuGetPackagesPath))
                {
                    Directory.Delete(NuGetPackagesPath, recursive: true);
                }
            }
            else
            {
                if (NuGetPackagesPath is not null && Directory.Exists(NuGetPackagesPath))
                {
                    foreach (var dir in Directory.GetDirectories(NuGetPackagesPath, "aspire*"))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
                Console.WriteLine($"*** Using NuGet cache (never deleted automatically): {NuGetPackagesPath}");
            }
        }

        TemplatesCustomHive = templatesCustomHive;
        TemplatesCustomHive?.EnsureInstalledAsync(this).Wait();

        static void CleanupTestRootPath()
        {
            if (!Directory.Exists(TestRootPath))
            {
                return;
            }

            try
            {
                Directory.Delete(TestRootPath, recursive: true);
            }
            catch (IOException) when (!EnvironmentVariables.IsRunningOnCI)
            {
                // there might be lingering processes that are holding onto the files
                // try deleting the subdirectories instead
                Console.WriteLine($"\tFailed to delete {TestRootPath} . Deleting subdirectories.");
                foreach (var dir in Directory.GetDirectories(TestRootPath))
                {
                    try
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                    catch (IOException ioex)
                    {
                        // ignore
                        Console.WriteLine($"\tFailed to delete {dir} : {ioex.Message}. Ignoring.");
                    }
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting '{TestRootPath}'.", ex);
            }
        }
    }

    public BuildEnvironment(BuildEnvironment otherBuildEnvironment)
    {
        DotNet = otherBuildEnvironment.DotNet;
        DefaultBuildArgs = otherBuildEnvironment.DefaultBuildArgs;
        EnvVars = new Dictionary<string, string>(otherBuildEnvironment.EnvVars);
        LogRootPath = otherBuildEnvironment.LogRootPath;
        BuiltNuGetsPath = otherBuildEnvironment.BuiltNuGetsPath;
        UsesCustomDotNet = otherBuildEnvironment.UsesCustomDotNet;
        NuGetPackagesPath = otherBuildEnvironment.NuGetPackagesPath;
        RepoRoot = otherBuildEnvironment.RepoRoot;
        TemplatesCustomHive = otherBuildEnvironment.TemplatesCustomHive;
    }

    private static TestTargetFramework ComputeDefaultTargetFramework()
        => EnvironmentVariables.DefaultTFMForTesting?.ToLowerInvariant() switch
        {
            null or "" or "net9.0" => TestTargetFramework.Current,
            "net8.0" => TestTargetFramework.Previous,
            _ => throw new ArgumentOutOfRangeException(nameof(EnvironmentVariables.DefaultTFMForTesting), EnvironmentVariables.DefaultTFMForTesting, "Invalid value")
        };

}

public enum TestTargetFramework
{
    // Current is default
    Current,
    Previous
}

public static class TestTargetFrameworkExtensions
{
    public static string ToTFMString(this TestTargetFramework tfm) => tfm switch
    {
        TestTargetFramework.Previous => "net8.0",
        TestTargetFramework.Current => "net9.0",
        _ => throw new ArgumentOutOfRangeException(nameof(tfm))
    };
}
