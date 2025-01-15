// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ResourceSourceViewModel(string value, string? contentAfterValue, string valueToVisualize, string tooltip)
{
    public string Value { get; } = value;
    public string? ContentAfterValue { get; } = contentAfterValue;
    public string ValueToVisualize { get; } = valueToVisualize;
    public string Tooltip { get; } = tooltip;

    internal static ResourceSourceViewModel? GetSourceViewModel(ResourceViewModel resource)
    {
        var executablePath = resource.TryGetExecutablePath(out var path) ? path : null;

        (string? ArgumentsString, string FullCommandLine)? commandLineInfo = null;

        if (resource.TryGetExecutableArguments(out var arguments))
        {
            var argumentsString = arguments.IsDefaultOrEmpty ? null : string.Join(" ", arguments);
            commandLineInfo = (ArgumentsString: argumentsString, $"{executablePath} {argumentsString}");
        }

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            if (commandLineInfo is { ArgumentsString: { } argumentsString, FullCommandLine: { } fullCommandLine })
            {
                return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: argumentsString, valueToVisualize: fullCommandLine, tooltip: fullCommandLine);
            }

            // default to project path if there is no executable path or executable arguments
            return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: commandLineInfo?.ArgumentsString, valueToVisualize: projectPath, tooltip: projectPath);
        }

        if (executablePath is not null)
        {
            return new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: commandLineInfo?.ArgumentsString, valueToVisualize: commandLineInfo?.FullCommandLine ?? executablePath, tooltip: commandLineInfo?.FullCommandLine ?? string.Empty);
        }

        if (resource.TryGetContainerImage(out var containerImage))
        {
            return new ResourceSourceViewModel(value: containerImage, contentAfterValue: null, valueToVisualize: containerImage, tooltip: containerImage);
        }

        if (resource.Properties.TryGetValue(KnownProperties.Resource.Source, out var property) && property.Value is { HasStringValue: true, StringValue: var value })
        {
            return new ResourceSourceViewModel(value, contentAfterValue: null, valueToVisualize: value, tooltip: value);
        }

        return null;
    }
}
