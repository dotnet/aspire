using Microsoft.Build.Locator;
using Microsoft.DotNet.Watch;

if (AspireWatchOptions.TryParse(args) is not { } options)
{
    return -1;
}

if (options.SdkDirectoryToRegister is { } sdkDirectory)
{
    MSBuildLocator.RegisterMSBuildPath(sdkDirectory);
}

return options switch
{
    AspireHostWatchOptions hostOptions => await AspireHostLauncher.LaunchAsync(Directory.GetCurrentDirectory(), hostOptions),
    AspireResourceWatchOptions resourceOptions => await AspireResourceLauncher.LaunchAsync(resourceOptions, CancellationToken.None),
    AspireServerWatchOptions serverOptions => await AspireServerLauncher.LaunchAsync(serverOptions),
    _ => -1,
};
