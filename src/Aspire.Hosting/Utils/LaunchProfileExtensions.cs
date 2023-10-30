// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Properties;

namespace Aspire.Hosting;

internal static class LaunchProfileExtensions
{
    internal static LaunchSettings? GetLaunchSettings(this ProjectResource projectResource)
    {
        if (!projectResource.TryGetLastAnnotation<IServiceMetadata>(out var serviceMetadata))
        {
            throw new DistributedApplicationException(Resources.ProjectDoesNotContainServiceMetadataExceptionMessage);
        }

        return serviceMetadata.GetLaunchSettings();
    }

    internal static LaunchProfile? GetEffectiveLaunchProfile(this ProjectResource projectResource)
    {
        string? launchProfileName = projectResource.SelectLaunchProfileName();
        if (string.IsNullOrEmpty(launchProfileName))
        {
            return null;
        }

        var profiles = projectResource.GetLaunchSettings()?.Profiles;
        if (profiles is null)
        {
            return null;
        }

        var found = profiles.TryGetValue(launchProfileName, out var launchProfile);
        return found == true ? launchProfile : null;
    }

    internal static LaunchSettings? GetLaunchSettings(this IServiceMetadata serviceMetadata)
    {
        if (!File.Exists(serviceMetadata.ProjectPath))
        {
            var message = string.Format(CultureInfo.InvariantCulture, Resources.ProjectFileNotFoundExceptionMessage, serviceMetadata.ProjectPath);
            throw new DistributedApplicationException(message);
        }

        var projectFileInfo = new FileInfo(serviceMetadata.ProjectPath);
        var launchSettingsFilePath = projectFileInfo.DirectoryName switch
        {
            null => Path.Combine("Properties", "launchSettings.json"),
            _ => Path.Combine(projectFileInfo.DirectoryName, "Properties", "launchSettings.json")
        };

        // It isn't mandatory that the launchSettings.json file exists!
        if (!File.Exists(launchSettingsFilePath))
        {
            return null;
        }

        using var stream = File.OpenRead(launchSettingsFilePath);
        var settings = JsonSerializer.Deserialize(stream, LaunchSetttingsSerializerContext.Default.LaunchSettings);
        return settings;
    }

    private static readonly LaunchProfileSelector[] s_launchProfileSelectors =
    [
        TrySelectLaunchProfileFromAnnotation,
        TrySelectLaunchProfileFromEnvironment,
        TrySelectLaunchProfileByOrder
    ];

    private static bool TrySelectLaunchProfileByOrder(ProjectResource projectResource, [NotNullWhen(true)] out string? launchProfileName)
    {
        var launchSettings = GetLaunchSettings(projectResource);

        if (launchSettings == null || launchSettings.Profiles.Count == 0)
        {
            launchProfileName = null;
            return false;
        }

        launchProfileName = launchSettings.Profiles.Keys.First();
        return true;
    }

    private static bool TrySelectLaunchProfileFromEnvironment(ProjectResource projectResource, [NotNullWhen(true)] out string? launchProfileName)
    {
        var launchProfileEnvironmentVariable = Environment.GetEnvironmentVariable("DOTNET_LAUNCH_PROFILE");

        if (launchProfileEnvironmentVariable is null)
        {
            launchProfileName = null;
            return false;
        }

        var launchSettings = GetLaunchSettings(projectResource);
        if (launchSettings == null)
        {
            launchProfileName = null;
            return false;
        }

        if (!launchSettings.Profiles.TryGetValue(launchProfileEnvironmentVariable, out var launchProfile))
        {
            launchProfileName = null;
            return false;
        }

        launchProfileName = launchProfileEnvironmentVariable;
        return launchProfile != null;
    }

    private static bool TrySelectLaunchProfileFromAnnotation(ProjectResource projectResource, [NotNullWhen(true)] out string? launchProfileName)
    {
        if (projectResource.TryGetLastAnnotation<LaunchProfileAnnotation>(out var launchProfileAnnotation))
        {
            launchProfileName = launchProfileAnnotation.LaunchProfileName;
            return true;
        }
        else
        {
            launchProfileName = null;
            return false;
        }
    }

    internal static string? SelectLaunchProfileName(this ProjectResource projectResource)
    {
        foreach (var launchProfileSelector in s_launchProfileSelectors)
        {
            if (launchProfileSelector(projectResource, out var launchProfile))
            {
                return launchProfile;
            }
        }

        return null;
    }
}

internal delegate bool LaunchProfileSelector(ProjectResource project, out string? launchProfile);

[JsonSerializable(typeof(LaunchSettings))]
internal sealed partial class LaunchSetttingsSerializerContext : JsonSerializerContext
{

}
