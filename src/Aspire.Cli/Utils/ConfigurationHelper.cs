// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Utils;

internal static class ConfigurationHelper
{
    internal static void RegisterSettingsFiles(IConfigurationBuilder configuration, DirectoryInfo workingDirectory, FileInfo globalSettingsFile)
    {
        var currentDirectory = workingDirectory;

        // Find the nearest local settings file
        FileInfo? localSettingsFile = null;

        while (currentDirectory is not null)
        {
            var settingsFilePath = BuildPathToSettingsJsonFile(currentDirectory.FullName);

            if (File.Exists(settingsFilePath))
            {
                localSettingsFile = new FileInfo(settingsFilePath);
                break;
            }

            currentDirectory = currentDirectory.Parent;
        }

        // Add global settings first (if it exists) - lower precedence
        if (File.Exists(globalSettingsFile.FullName))
        {
            configuration.AddJsonFile(globalSettingsFile.FullName, optional: true);
        }

        // Then add local settings (if found) - this will override global settings
        if (localSettingsFile is not null)
        {
            configuration.AddJsonFile(localSettingsFile.FullName, optional: true);
        }
    }

    internal static string BuildPathToSettingsJsonFile(string workingDirectory)
    {
        return Path.Combine(workingDirectory, ".aspire", "settings.json");
    }
}
