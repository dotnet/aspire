// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ResourceSourceViewModel(string value, List<LaunchArgument>? contentAfterValue, string valueToVisualize, string tooltip)
{
    public string Value { get; } = value;
    public List<LaunchArgument>? ContentAfterValue { get; } = contentAfterValue;
    public string ValueToVisualize { get; } = valueToVisualize;
    public string Tooltip { get; } = tooltip;

    internal static ResourceSourceViewModel? GetSourceViewModel(ResourceViewModel resource)
    {
        (List<LaunchArgument>? Arguments, string? ArgumentsString)? commandLineInfo;

        // If the resource contains launch arguments, these project arguments should be shown in place of all executable arguments,
        // which include args added by the app host
        if (resource.TryGetAppArgs(out var launchArguments))
        {
            if (launchArguments.IsDefaultOrEmpty)
            {
                commandLineInfo = (null, null);
            }
            else
            {
                var argumentsString = string.Join(" ", launchArguments);
                if (resource.TryGetAppArgsSensitivity(out var areArgumentsSensitive))
                {
                    var arguments = launchArguments
                        .Select((arg, i) => new LaunchArgument(arg, IsShown: !areArgumentsSensitive[i]))
                        .ToList();

                    commandLineInfo = (Arguments: arguments, argumentsString);
                }
                else
                {
                    commandLineInfo = (Arguments: launchArguments.Select(arg => new LaunchArgument(arg, true)).ToList(), argumentsString);
                }
            }
        }
        else if (resource.TryGetExecutableArguments(out var executableArguments) && !resource.IsProject())
        {
            var arguments = executableArguments.IsDefaultOrEmpty ? null : executableArguments.Select(arg => new LaunchArgument(arg, true)).ToList();
            commandLineInfo = (Arguments: arguments, string.Join(' ', executableArguments));
        }
        else
        {
            commandLineInfo = (Arguments: null, null);
        }

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            if (commandLineInfo is { Arguments: { } arguments, ArgumentsString: { } fullCommandLine })
            {
                return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: arguments, valueToVisualize: $"{projectPath} {fullCommandLine}", tooltip: $"{projectPath} {fullCommandLine}");
            }

            // default to project path if there is no executable path or executable arguments
            return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: projectPath, tooltip: projectPath);
        }

        if (resource.TryGetExecutablePath(out var executablePath))
        {
            var fullSource = commandLineInfo?.ArgumentsString is not null ? $"{executablePath} {commandLineInfo.Value.ArgumentsString}" : executablePath;
            return new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: fullSource, tooltip: fullSource);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            var fullSource = commandLineInfo?.ArgumentsString is null ? containerImage : $"{containerImage} {commandLineInfo.Value.ArgumentsString}";
            return new ResourceSourceViewModel(value: containerImage, contentAfterValue: commandLineInfo?.Arguments, valueToVisualize: fullSource, tooltip: fullSource);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return new ResourceSourceViewModel(value, contentAfterValue: null, valueToVisualize: value, tooltip: value);
        }

        return null;
    }
}

public record LaunchArgument(string Value, bool IsShown);
