// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.CommandLine;
using System.Threading.Channels;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireServerLauncher(
    GlobalOptions globalOptions,
    EnvironmentOptions environmentOptions,
    string serverPipeName,
    ImmutableArray<string> resourcePaths,
    string? statusPipeName,
    string? controlPipeName)
    : AspireWatcherLauncher(globalOptions, environmentOptions)
{
    private const string LogMessagePrefix = "aspire watch server";

    public string ServerPipeName => serverPipeName;
    public ImmutableArray<string> ResourcePaths => resourcePaths;
    public string? StatusPipeName => statusPipeName;
    public string? ControlPipeName => controlPipeName;

    public static AspireServerLauncher? TryCreate(ParseResult parseResult, AspireServerCommandDefinition command)
    {
        var serverPipeName = parseResult.GetValue(command.ServerOption)!;
        var sdkDirectory = parseResult.GetValue(command.SdkOption)!;
        var resourcePaths = parseResult.GetValue(command.ResourceOption) ?? [];
        var statusPipeName = parseResult.GetValue(command.StatusPipeOption);
        var controlPipeName = parseResult.GetValue(command.ControlPipeOption);

        var globalOptions = new GlobalOptions()
        {
            LogLevel = command.GetLogLevel(parseResult),
            NoHotReload = false,
            NonInteractive = true,
        };

        return new AspireServerLauncher(
            globalOptions,
            EnvironmentOptions.FromEnvironment(sdkDirectory, LogMessagePrefix),
            serverPipeName: serverPipeName,
            resourcePaths: [.. resourcePaths],
            statusPipeName: statusPipeName,
            controlPipeName: controlPipeName);
    }

    public override async Task<int> LaunchAsync(CancellationToken cancellationToken)
    {
        await using var statusWriter = StatusPipeName != null ? new WatchStatusWriter(StatusPipeName, Logger) : null;

        var processLauncherFactory = new ProcessLauncherFactory(ServerPipeName, ControlPipeName, statusWriter, launchProfile: null, cancellationToken);

        return await LaunchWatcherAsync(
            rootProjects: [.. ResourcePaths.Select(ProjectRepresentation.FromProjectOrEntryPointFilePath)],
            statusWriter != null ? new StatusReportingLoggerFactory(statusWriter, LoggerFactory) : LoggerFactory,
            processLauncherFactory,
            cancellationToken);
    }
}
