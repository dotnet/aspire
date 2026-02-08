// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal static class AspireServerLauncher
{
    public static async Task<int> LaunchAsync(AspireServerWatchOptions options)
    {
        var globalOptions = new GlobalOptions()
        {
            LogLevel = options.LogLevel,
            NoHotReload = false,
            NonInteractive = true,
        };

        var muxerPath = Path.GetFullPath(Path.Combine(options.SdkDirectory, "..", "..", "dotnet" + PathUtilities.ExecutableExtension));

        // msbuild tasks depend on host path variable:
        Environment.SetEnvironmentVariable(EnvironmentVariables.Names.DotnetHostPath, muxerPath);

        var console = new PhysicalConsole(TestFlags.None);
        var reporter = new ConsoleReporter(console, suppressEmojis: false);
        var environmentOptions = EnvironmentOptions.FromEnvironment(muxerPath);
        var processRunner = new ProcessRunner(environmentOptions.GetProcessCleanupTimeout());
        var loggerFactory = new LoggerFactory(reporter, globalOptions.LogLevel);
        var logger = loggerFactory.CreateLogger(DotNetWatchContext.DefaultLogComponentName);

        // Connect to status pipe if provided
        WatchStatusWriter? statusWriter = null;
        if (options.StatusPipeName is { } statusPipeName)
        {
            statusWriter = await WatchStatusWriter.TryConnectAsync(statusPipeName, logger, CancellationToken.None);
        }

        await using var statusWriterDispose = statusWriter;

        // Connect to control pipe if provided
        WatchControlReader? controlReader = null;
        if (options.ControlPipeName is { } controlPipeName)
        {
            controlReader = await WatchControlReader.TryConnectAsync(controlPipeName, logger, CancellationToken.None);
        }

        await using var controlReaderDispose = controlReader;

        using var context = new DotNetWatchContext()
        {
            ProcessOutputReporter = reporter,
            LoggerFactory = loggerFactory,
            Logger = logger,
            BuildLogger = loggerFactory.CreateLogger(DotNetWatchContext.BuildLogComponentName),
            ProcessRunner = processRunner,
            Options = globalOptions,
            EnvironmentOptions = environmentOptions,
            RootProjectOptions = null,
            BuildArguments = [], // TODO?
            TargetFramework = null, // TODO?
            LaunchProfileName = null, // TODO: options.NoLaunchProfile ? null : options.LaunchProfileName,
            RootProjects = [.. options.ResourcePaths.Select(ProjectRepresentation.FromProjectOrEntryPointFilePath)],
            BrowserRefreshServerFactory = new BrowserRefreshServerFactory(),
            BrowserLauncher = new BrowserLauncher(logger, reporter, environmentOptions),
            StatusEventWriter = statusWriter is not null ? statusWriter.WriteEventAsync : null,
        };

        using var shutdownHandler = new ShutdownHandler(console, logger);

        try
        {
            var processLauncherFactory = new ProcessLauncherFactory(options.ServerPipeName, context.StatusEventWriter, shutdownHandler.CancellationToken);
            var watcher = new HotReloadDotNetWatcher(context, console, processLauncherFactory);

            if (controlReader is not null)
            {
                _ = ListenForControlCommandsAsync(controlReader, watcher, logger, shutdownHandler.CancellationToken);
            }

            await watcher.WatchAsync(shutdownHandler.CancellationToken);
        }
        catch (OperationCanceledException) when (shutdownHandler.CancellationToken.IsCancellationRequested)
        {
            // Ctrl+C forced an exit
        }
        catch (Exception e)
        {
            logger.LogError("An unexpected error occurred: {Exception}", e.ToString());
            return -1;
        }

        return 0;
    }

    static async Task ListenForControlCommandsAsync(
        WatchControlReader reader, HotReloadDotNetWatcher watcher,
        ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var command = await reader.ReadCommandAsync(cancellationToken);
                if (command is null)
                {
                    break;
                }

                logger.LogInformation("Received control command: {Type}", command.Type);

                if (command.Type == WatchControlCommand.Types.Rebuild)
                {
                    watcher.RequestRestart();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogDebug("Control pipe listener ended: {Message}", ex.Message);
        }
    }
}
