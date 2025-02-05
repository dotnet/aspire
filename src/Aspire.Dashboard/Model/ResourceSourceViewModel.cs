// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Aspire.Dashboard.Model;

public class ResourceSourceViewModel(string value, List<LaunchArgument>? contentAfterValue, string valueToVisualize, string tooltip)
{
    public string Value { get; } = value;
    public List<LaunchArgument>? ContentAfterValue { get; } = contentAfterValue;
    public string ValueToVisualize { get; } = valueToVisualize;
    public string Tooltip { get; } = tooltip;

    internal static ResourceSourceViewModel? GetSourceViewModel(ResourceViewModel resource)
    {
        var executablePath = resource.TryGetExecutablePath(out var path) ? path : null;
        var projectPath = resource.TryGetProjectPath(out var projPath) ? projPath : null;

        (List<LaunchArgument>? Arguments, string FullCommandLine)? commandLineInfo = null;

        // If the resource contains launch arguments, these project arguments should be shown in place of all executable arguments,
        // which include args added by the app host
        if (resource.TryGetAppArgs(out var launchArguments))
        {
            if (launchArguments.IsDefaultOrEmpty)
            {
                commandLineInfo = (null, executablePath ?? string.Empty);
            }
            else
            {
                var programPath = projectPath ?? executablePath;
                var argumentsString = string.Join(" ", launchArguments);
                if (resource.TryGetAppArgsFormatParams(out var formatParams))
                {
                    var arguments = launchArguments
                        .Select(arg => new LaunchArgument(arg, !Regex.IsMatch(arg, "{\\d+}")))
                        .ToList();
                    var launchArgsString = string.Format(CultureInfo.InvariantCulture, argumentsString, [.. formatParams]);
                    commandLineInfo = (Arguments: arguments, programPath is null ? argumentsString : $"{programPath} {launchArgsString}");
                }
                else
                {
                    commandLineInfo = (Arguments: launchArguments.Select(arg => new LaunchArgument(arg, true)).ToList(), programPath is null ? argumentsString : $"{programPath} {argumentsString}");
                }
            }
        }
        else if (resource.TryGetExecutableArguments(out var executableArguments) && !resource.IsProject())
        {
            var arguments = executableArguments.IsDefaultOrEmpty ? null : executableArguments.Select(arg => new LaunchArgument(arg, true)).ToList();
            commandLineInfo = (Arguments: arguments, $"{executablePath} {string.Join(' ', executableArguments)}");
        }
        else
        {
            commandLineInfo = (Arguments: null, projectPath ?? executablePath ?? string.Empty);
        }

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && projectPath is not null)
        {
            if (commandLineInfo is { Arguments: { } arguments, FullCommandLine: { } fullCommandLine })
            {
                return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: arguments, valueToVisualize: fullCommandLine, tooltip: fullCommandLine);
            }

            // default to project path if there is no executable path or executable arguments
            return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: projectPath, tooltip: projectPath);
        }

        if (executablePath is not null)
        {
            return new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: commandLineInfo?.FullCommandLine ?? executablePath, tooltip: commandLineInfo?.FullCommandLine ?? string.Empty);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            return new ResourceSourceViewModel(value: containerImage, contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: containerImage, tooltip: containerImage);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return new ResourceSourceViewModel(value, contentAfterValue: null, valueToVisualize: value, tooltip: value);
        }

        return null;
    }
}

public record LaunchArgument(string Value, bool IsShown);
