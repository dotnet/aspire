// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.DotNet.ProjectTools;

internal sealed class ProjectLaunchProfileParser : LaunchProfileParser
{
    public const string CommandName = "Project";

    public static readonly ProjectLaunchProfileParser Instance = new();

    private ProjectLaunchProfileParser()
    {
    }

    public override LaunchProfileParseResult ParseProfile(string launchSettingsPath, string? launchProfileName, string json)
    {
        var profile = JsonSerializer.Deserialize<ProjectLaunchProfile>(json);
        if (profile == null)
        {
            return LaunchProfileParseResult.Failure(Resources.LaunchProfileIsNotAJsonObject);
        }

        return LaunchProfileParseResult.Success(new ProjectLaunchProfile
        {
            LaunchProfileName = launchProfileName,
            CommandLineArgs = ParseCommandLineArgs(profile.CommandLineArgs),
            LaunchBrowser = profile.LaunchBrowser,
            LaunchUrl = profile.LaunchUrl,
            ApplicationUrl = profile.ApplicationUrl,
            DotNetRunMessages = profile.DotNetRunMessages,
            EnvironmentVariables = ParseEnvironmentVariables(profile.EnvironmentVariables),
        });
    }
}
