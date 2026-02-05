// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.DotNet.ProjectTools;

internal sealed class ExecutableLaunchProfileParser : LaunchProfileParser
{
    public const string CommandName = "Executable";

    public static readonly ExecutableLaunchProfileParser Instance = new();

    private ExecutableLaunchProfileParser()
    {
    }

    public override LaunchProfileParseResult ParseProfile(string launchSettingsPath, string? launchProfileName, string json)
    {
        var profile = JsonSerializer.Deserialize<ExecutableLaunchProfile>(json);
        if (profile == null)
        {
            return LaunchProfileParseResult.Failure(Resources.LaunchProfileIsNotAJsonObject);
        }

        if (!TryParseWorkingDirectory(launchSettingsPath, profile.WorkingDirectory, out var workingDirectory, out var error))
        {
            return LaunchProfileParseResult.Failure(error);
        }

        return LaunchProfileParseResult.Success(new ExecutableLaunchProfile
        {
            LaunchProfileName = launchProfileName,
            ExecutablePath = ExpandVariables(profile.ExecutablePath),
            CommandLineArgs = ParseCommandLineArgs(profile.CommandLineArgs),
            WorkingDirectory = workingDirectory,
            DotNetRunMessages = profile.DotNetRunMessages,
            EnvironmentVariables = ParseEnvironmentVariables(profile.EnvironmentVariables),
        });
    }

    private static bool TryParseWorkingDirectory(string launchSettingsPath, string? value, out string? workingDirectory, [NotNullWhen(false)] out string? error)
    {
        if (value == null)
        {
            workingDirectory = null;
            error = null;
            return true;
        }

        var expandedValue = ExpandVariables(value);

        try
        {
            workingDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(launchSettingsPath)!, expandedValue));
            error = null;
            return true;
        }
        catch
        {
            workingDirectory = null;
            error = string.Format(Resources.Path0SpecifiedIn1IsInvalid, expandedValue, ExecutableLaunchProfile.WorkingDirectoryPropertyName);
            return false;
        }
    }
}
