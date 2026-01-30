using Microsoft.Build.Locator;
using Microsoft.DotNet.Watch;

if (AspireWatchOptions.TryParse(args) is not { } options)
{
    return -1;
}

MSBuildLocator.RegisterMSBuildPath(options.SdkDirectory);

var workingDirectory = Directory.GetCurrentDirectory();
return await AspireHostLauncher.RunAsync(workingDirectory, options) ? 0 : 1;
