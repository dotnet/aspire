// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Model;

public class ResourceSourceViewModel(string value, List<LaunchArgument>? contentAfterValue, string valueToVisualize, string tooltip)
{
    public string Value { get; } = value;
    public List<LaunchArgument>? ContentAfterValue { get; } = contentAfterValue;
    public string ValueToVisualize { get; } = valueToVisualize;
    public string Tooltip { get; } = tooltip;

    internal static ResourceSourceViewModel? GetSourceViewModel(ResourceViewModel resource)
    {
        var commandLineInfo = GetCommandLineInfo(resource);

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            return CreateResourceSourceViewModel(Path.GetFileName(projectPath), projectPath, commandLineInfo);
        }

        if (resource.TryGetExecutablePath(out var executablePath))
        {
            return CreateResourceSourceViewModel(Path.GetFileName(executablePath), executablePath, commandLineInfo);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            return CreateResourceSourceViewModel(containerImage, containerImage, commandLineInfo);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return new ResourceSourceViewModel(value, contentAfterValue: null, valueToVisualize: value, tooltip: value);
        }

        return null;

        static CommandLineInfo? GetCommandLineInfo(ResourceViewModel resourceViewModel)
        {
            // If the resource contains launch arguments, these project arguments should be shown in place of all executable arguments,
            // which include args added by the app host
            if (resourceViewModel.TryGetAppArgs(out var launchArguments))
            {
                if (launchArguments.IsDefaultOrEmpty)
                {
                    return null;
                }

                var argumentsString = string.Join(" ", launchArguments);
                if (resourceViewModel.TryGetAppArgsSensitivity(out var areArgumentsSensitive))
                {
                    var arguments = launchArguments
                        .Select((arg, i) => new LaunchArgument(arg, IsShown: !areArgumentsSensitive[i]))
                        .ToList();

                    return new CommandLineInfo(
                        Arguments: arguments,
                        ArgumentsString: argumentsString,
                        TooltipString: string.Join(" ", arguments.Select(arg => arg.IsShown
                            ? arg.Value
                            : DashboardUIHelpers.GetMaskingText(6).Text)));
                }

                return new CommandLineInfo(Arguments: launchArguments.Select(arg => new LaunchArgument(arg, true)).ToList(), ArgumentsString: argumentsString, TooltipString: argumentsString);
            }

            if (resourceViewModel.TryGetExecutableArguments(out var executableArguments) && !resourceViewModel.IsProject())
            {
                var arguments = executableArguments.IsDefaultOrEmpty ? [] : executableArguments.Select(arg => new LaunchArgument(arg, true)).ToList();
                var argumentsString = string.Join(" ", executableArguments);

                return new CommandLineInfo(Arguments: arguments, ArgumentsString: argumentsString, TooltipString: argumentsString);
            }

            return null;
        }

        static ResourceSourceViewModel CreateResourceSourceViewModel(string value, string path, CommandLineInfo? commandLineInfo)
        {
            return commandLineInfo is not null
                ? new ResourceSourceViewModel(value: value, contentAfterValue: commandLineInfo.Arguments, valueToVisualize: $"{path} {commandLineInfo.ArgumentsString}", tooltip: $"{path} {commandLineInfo.TooltipString}")
                : new ResourceSourceViewModel(value: value, contentAfterValue: null, valueToVisualize: path, tooltip: path);
        }
    }

    private record CommandLineInfo(List<LaunchArgument> Arguments, string ArgumentsString, string TooltipString);
}

public record LaunchArgument(string Value, bool IsShown);
