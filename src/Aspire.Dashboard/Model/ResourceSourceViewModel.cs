// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
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
        CommandLineInfo? commandLineInfo;

        // If the resource contains launch arguments, these project arguments should be shown in place of all executable arguments,
        // which include args added by the app host
        if (resource.TryGetAppArgs(out var launchArguments))
        {
            if (launchArguments.IsDefaultOrEmpty)
            {
                commandLineInfo = null;
            }
            else
            {
                var argumentsString = string.Join(" ", launchArguments);
                if (resource.TryGetAppArgsSensitivity(out var areArgumentsSensitive))
                {
                    var arguments = launchArguments
                        .Select((arg, i) => new LaunchArgument(arg, IsShown: !areArgumentsSensitive[i]))
                        .ToList();

                    commandLineInfo = new CommandLineInfo(
                        Arguments: arguments,
                        ArgumentsString: argumentsString,
                        TooltipString: string.Join(" ", arguments.Select(arg => arg.IsShown
                            ? arg.Value
                            : WebUtility.HtmlDecode(DashboardUIHelpers.GetMaskingText(6).Value))));
                }
                else
                {
                    commandLineInfo = new CommandLineInfo(Arguments: launchArguments.Select(arg => new LaunchArgument(arg, true)).ToList(), ArgumentsString: argumentsString, TooltipString: argumentsString);
                }
            }
        }
        else if (resource.TryGetExecutableArguments(out var executableArguments) && !resource.IsProject())
        {
            var arguments = executableArguments.IsDefaultOrEmpty ? [] : executableArguments.Select(arg => new LaunchArgument(arg, true)).ToList();
            var argumentsString = string.Join(" ", executableArguments);

            commandLineInfo = new CommandLineInfo(Arguments: arguments, ArgumentsString: argumentsString, TooltipString: argumentsString);
        }
        else
        {
            commandLineInfo = null;
        }

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            return commandLineInfo is not null
                ? new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: commandLineInfo.Arguments, valueToVisualize: $"{projectPath} {commandLineInfo.ArgumentsString}", tooltip: $"{projectPath} {commandLineInfo.TooltipString}")
                // default to project path if there is no executable path or executable arguments
                : new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: null, valueToVisualize: projectPath, tooltip: projectPath);
        }

        if (resource.TryGetExecutablePath(out var executablePath))
        {
            return commandLineInfo is not null
                ? new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: commandLineInfo.Arguments, valueToVisualize: $"{executablePath} {commandLineInfo.ArgumentsString}", tooltip: $"{executablePath} {commandLineInfo.TooltipString}")
                : new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: null, valueToVisualize: executablePath, tooltip: executablePath);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            return commandLineInfo is not null
                ? new ResourceSourceViewModel(value: containerImage, contentAfterValue: commandLineInfo.Arguments, valueToVisualize: $"{containerImage} {commandLineInfo.ArgumentsString}", tooltip: $"{containerImage} {commandLineInfo.TooltipString}")
                : new ResourceSourceViewModel(value: containerImage, contentAfterValue: null, valueToVisualize: containerImage, tooltip: containerImage);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return new ResourceSourceViewModel(value, contentAfterValue: null, valueToVisualize: value, tooltip: value);
        }

        return null;
    }

    private record CommandLineInfo(List<LaunchArgument> Arguments, string ArgumentsString, string TooltipString);
}

public record LaunchArgument(string Value, bool IsShown);
