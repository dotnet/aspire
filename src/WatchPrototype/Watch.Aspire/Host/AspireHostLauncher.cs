// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class AspireHostLauncher(
    GlobalOptions globalOptions,
    EnvironmentOptions environmentOptions,
    ProjectRepresentation entryPoint,
    ImmutableArray<string> applicationArguments,
    Optional<string?> launchProfileName)
    : AspireWatcherLauncher(globalOptions, environmentOptions)
{
    internal const string LogMessagePrefix = "aspire watch host";

    public ProjectRepresentation EntryPoint => entryPoint;
    public ImmutableArray<string> ApplicationArguments => applicationArguments;
    public Optional<string?> LaunchProfileName => launchProfileName;

    internal static AspireHostLauncher? TryCreate(ParseResult parseResult, AspireHostCommandDefinition command)
    {
        var sdkDirectory = parseResult.GetValue(command.SdkOption)!;
        var entryPointPath = parseResult.GetValue(command.EntryPointOption)!;
        var applicationArguments = parseResult.GetValue(command.ApplicationArguments) ?? [];
        var launchProfile = parseResult.GetValue(command.LaunchProfileOption);
        var noLaunchProfile = parseResult.GetValue(command.NoLaunchProfileOption);

        var globalOptions = new GlobalOptions()
        {
            LogLevel = command.GetLogLevel(parseResult),
            NoHotReload = false,
            NonInteractive = true,
        };

        return new AspireHostLauncher(
            globalOptions,
            EnvironmentOptions.FromEnvironment(sdkDirectory, LogMessagePrefix),
            entryPoint: ProjectRepresentation.FromProjectOrEntryPointFilePath(entryPointPath),
            applicationArguments: [.. applicationArguments],
            launchProfileName: noLaunchProfile ? Optional<string?>.NoValue : launchProfile);
    }

    internal ProjectOptions GetProjectOptions()
    {
        var commandArguments = new List<string>()
        {
             EntryPoint.IsProjectFile ? "--project" : "--file",
             EntryPoint.ProjectOrEntryPointFilePath,
        };

        if (LaunchProfileName.Value != null)
        {
            commandArguments.Add("--launch-profile");
            commandArguments.Add(LaunchProfileName.Value);
        }
        else if (!LaunchProfileName.HasValue)
        {
            commandArguments.Add("--no-launch-profile");
        }

        commandArguments.AddRange(ApplicationArguments);

        return new ProjectOptions()
        {
            IsMainProject = true,
            Representation = EntryPoint,
            WorkingDirectory = EnvironmentOptions.WorkingDirectory,
            LaunchProfileName = LaunchProfileName,
            Command = "run",
            CommandArguments = [.. commandArguments],
            LaunchEnvironmentVariables = [],
        };
    }

    public override async Task<int> LaunchAsync(CancellationToken cancellationToken)
    {
        return await LaunchWatcherAsync(
            rootProjects: [EntryPoint],
            LoggerFactory,
            processLauncherFactory: null,
            cancellationToken);
    }
}
