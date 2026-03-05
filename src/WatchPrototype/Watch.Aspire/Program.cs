using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.DotNet.Watch;

try
{
    if (AspireLauncher.TryCreate(args) is not { } launcher)
    {
        return -1;
    }

    if (launcher.EnvironmentOptions.SdkDirectory != null)
    {
        MSBuildLocator.RegisterMSBuildPath(launcher.EnvironmentOptions.SdkDirectory);

        // msbuild tasks depend on host path variable:
        Environment.SetEnvironmentVariable(EnvironmentVariables.Names.DotnetHostPath, launcher.EnvironmentOptions.GetMuxerPath());
    }

    return await launcher.LaunchAsync(CancellationToken.None);
}
catch (Exception e)
{
    Console.Error.WriteLine($"Unexpected exception: {e}");
    return -1;
}
