// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Dashboard.Model;

public class ResourceSourceViewModel(string value, string? contentAfterValue, string valueToVisualize, string tooltip)
{
    public string Value { get; } = value;
    public string? ContentAfterValue { get; } = contentAfterValue;
    public string ValueToVisualize { get; } = valueToVisualize;
    public string Tooltip { get; } = tooltip;

    internal static ResourceSourceViewModel? GetSourceViewModel(ResourceViewModel resource)
    {
        string? executablePath;
        (string? NonDefaultArguments, string FullCommandLine)? commandLineInfo;

        if (resource.TryGetExecutablePath(out var path))
        {
            executablePath = path;
            commandLineInfo = GetCommandLineInfo(resource, executablePath);
        }
        else
        {
            executablePath = null;
            commandLineInfo = null;
        }

        // NOTE projects are also executables, so we have to check for projects first
        if (resource.IsProject() && resource.TryGetProjectPath(out var projectPath))
        {
            if (commandLineInfo is { NonDefaultArguments: { } argumentsString, FullCommandLine: { } fullCommandLine })
            {
                return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: argumentsString, valueToVisualize: fullCommandLine, tooltip: fullCommandLine);
            }

            // default to project path if there is no executable path or executable arguments
            return new ResourceSourceViewModel(value: Path.GetFileName(projectPath), contentAfterValue: commandLineInfo?.NonDefaultArguments, valueToVisualize: projectPath, tooltip: projectPath);
        }

        if (executablePath is not null)
        {
            return new ResourceSourceViewModel(value: Path.GetFileName(executablePath), contentAfterValue: commandLineInfo?.NonDefaultArguments, valueToVisualize: commandLineInfo?.FullCommandLine ?? executablePath, tooltip: commandLineInfo?.FullCommandLine ?? string.Empty);
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

    /**
     * Returns information about command line arguments, stripping out DCP default arguments, if any exist.
     * The defaults come from DcpExecutor#PrepareProjectExecutables and need to be kept in sync
     */
    private static (string? NonDefaultArguments, string FullCommandLine)? GetCommandLineInfo(ResourceViewModel resource, string executablePath)
    {
        if (resource.TryGetExecutableArguments(out var arguments))
        {
            if (arguments.IsDefaultOrEmpty)
            {
                return (NonDefaultArguments: null, FullCommandLine: executablePath);
            }

            var escapedArguments = arguments.Select(EscapeCommandLineArgument).ToList();

            if (resource.IsProject())
            {
                if (escapedArguments.Count > 3 && escapedArguments.Take(3).SequenceEqual(["run", "--no-build", "--project"], StringComparers.CommandLineArguments))
                {
                    escapedArguments.RemoveRange(0, 4); // remove the project path too
                }
                else if (escapedArguments.Count > 4 && escapedArguments.Take(4).SequenceEqual(["watch", "--non-interactive", "--no-hot-reload", "--project"], StringComparers.CommandLineArguments))
                {
                    escapedArguments.RemoveRange(0, 5); // remove the project path too
                }

                if (escapedArguments.Count > 1 && string.Equals(escapedArguments[0], "-c", StringComparisons.CommandLineArguments))
                {
                    escapedArguments.RemoveRange(0, 2);
                }

                if (escapedArguments.Count > 0 && string.Equals(escapedArguments[0], "--no-launch-profile", StringComparisons.CommandLineArguments))
                {
                    escapedArguments.RemoveAt(0);
                }
            }

            var cleanedArguments = escapedArguments.Count == 0 ? null : string.Join(' ', escapedArguments);
            var fullCommandLine = resource.TryGetProjectPath(out var projectPath)
                ? AppendArgumentsIfNotEmpty(projectPath, cleanedArguments)
                : AppendArgumentsIfNotEmpty(executablePath, cleanedArguments);

            return (NonDefaultArguments: cleanedArguments ?? string.Empty, FullCommandLine: fullCommandLine);

            static string AppendArgumentsIfNotEmpty(string s, string? arguments) => arguments is null ? s : $"{s} {arguments}";
        }

        return null;

        // This method doesn't account for all cases, but does the most common
        static string EscapeCommandLineArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            if (argument.Contains(' ') || argument.Contains('"') || argument.Contains('\\'))
            {
                var escapedArgument = new StringBuilder();
                escapedArgument.Append('"');

                for (int i = 0; i < argument.Length; i++)
                {
                    char c = argument[i];
                    switch (c)
                    {
                        case '\\':
                            // Escape backslashes
                            escapedArgument.Append('\\');
                            escapedArgument.Append('\\');
                            break;
                        case '"':
                            // Escape quotes
                            escapedArgument.Append('\\');
                            escapedArgument.Append('"');
                            break;
                        default:
                            escapedArgument.Append(c);
                            break;
                    }
                }

                escapedArgument.Append('"');
                return escapedArgument.ToString();
            }

            return argument;
        }
    }
}
