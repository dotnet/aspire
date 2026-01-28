// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal static class DotNetWatchLauncher
{
    public static async Task<bool> RunAsync(string workingDirectory, DotNetWatchOptions options)
    {
        var globalOptions = new GlobalOptions()
        {
            LogLevel = options.LogLevel,
            NoHotReload = false,
            NonInteractive = true,
        };

        var commandArguments = new List<string>();
        if (options.NoLaunchProfile)
        {
            commandArguments.Add("--no-launch-profile");
        }

        commandArguments.AddRange(options.ApplicationArguments);

        var rootProjectOptions = new ProjectOptions()
        {
            IsRootProject = true,
            ProjectPath = options.ProjectPath,
            WorkingDirectory = workingDirectory,
            TargetFramework = null,
            BuildArguments = [],
            NoLaunchProfile = options.NoLaunchProfile,
            LaunchProfileName = null,
            Command = "run",
            CommandArguments = [.. commandArguments],
            LaunchEnvironmentVariables = [],
        };

        var muxerPath = Path.GetFullPath(Path.Combine(options.SdkDirectory, "..", "..", "dotnet" + PathUtilities.ExecutableExtension));

        // msbuild tasks depend on host path variable:
        Environment.SetEnvironmentVariable(EnvironmentVariables.Names.DotnetHostPath, muxerPath);

        var console = new PhysicalConsole(TestFlags.None);
        var reporter = new ConsoleReporter(console, suppressEmojis: false);
        var environmentOptions = EnvironmentOptions.FromEnvironment(muxerPath);
        var processRunner = new ProcessRunner(environmentOptions.GetProcessCleanupTimeout(isHotReloadEnabled: true));
        var loggerFactory = new LoggerFactory(reporter, globalOptions.LogLevel);
        var logger = loggerFactory.CreateLogger(DotNetWatchContext.DefaultLogComponentName);

        using var context = new DotNetWatchContext()
        {
            ProcessOutputReporter = reporter,
            LoggerFactory = loggerFactory,
            Logger = logger,
            BuildLogger = loggerFactory.CreateLogger(DotNetWatchContext.BuildLogComponentName),
            ProcessRunner = processRunner,
            Options = globalOptions,
            EnvironmentOptions = environmentOptions,
            RootProjectOptions = rootProjectOptions,
            BrowserRefreshServerFactory = new BrowserRefreshServerFactory(),
            BrowserLauncher = new BrowserLauncher(logger, reporter, environmentOptions),
        };

        using var shutdownHandler = new ShutdownHandler(console, logger);

        try
        {
            var watcher = new HotReloadDotNetWatcher(context, console, runtimeProcessLauncherFactory: null);
            await watcher.WatchAsync(shutdownHandler.CancellationToken);
        }
        catch (OperationCanceledException) when (shutdownHandler.CancellationToken.IsCancellationRequested)
        {
            // Ctrl+C forced an exit
        }
        catch (Exception e)
        {
            logger.LogError("An unexpected error occurred: {Exception}", e.ToString());
            return false;
        }

        return true;
    }
}
