// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Default implementation of <see cref="IAspireTempDirectoryService"/>.
/// </summary>
internal sealed class AspireTempDirectoryService : IAspireTempDirectoryService
{
    private const string EnvironmentVariableName = "ASPIRE_TEMP_FOLDER";
    private const string ConfigurationKeyName = "Aspire:TempDirectory";
    private const string DefaultSubdirectory = ".aspire/temp";

    private readonly string _baseTempDirectory;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="AspireTempDirectoryService"/>.
    /// </summary>
    /// <param name="configuration">Configuration to read settings from.</param>
    public AspireTempDirectoryService(IConfiguration? configuration = null)
    {
        _baseTempDirectory = ResolveBaseTempDirectory(configuration);
        EnsureDirectoryExists(_baseTempDirectory);
    }

    /// <inheritdoc/>
    public string BaseTempDirectory => _baseTempDirectory;

    /// <inheritdoc/>
    public string CreateTempSubdirectory(string? prefix = null)
    {
        lock (_lock)
        {
            var subdirectoryName = string.IsNullOrEmpty(prefix)
                ? Guid.NewGuid().ToString("N")
                : $"{prefix}-{Guid.NewGuid():N}";

            var subdirectoryPath = Path.Combine(_baseTempDirectory, subdirectoryName);
            EnsureDirectoryExists(subdirectoryPath);
            return subdirectoryPath;
        }
    }

    /// <inheritdoc/>
    public string GetTempFilePath(string? extension = null)
    {
        lock (_lock)
        {
            var fileName = Guid.NewGuid().ToString("N");
            if (!string.IsNullOrEmpty(extension))
            {
                // Ensure extension starts with a dot
                if (!extension.StartsWith('.'))
                {
                    extension = "." + extension;
                }
                fileName += extension;
            }

            return Path.Combine(_baseTempDirectory, fileName);
        }
    }

    /// <inheritdoc/>
    public string GetTempSubdirectoryPath(string subdirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);
        return Path.Combine(_baseTempDirectory, subdirectory);
    }

    private static string ResolveBaseTempDirectory(IConfiguration? configuration)
    {
        // Priority:
        // 1. Environment variable ASPIRE_TEMP_FOLDER
        // 2. Configuration Aspire:TempDirectory
        // 3. Default: ~/.aspire/temp

        var envVar = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(envVar))
        {
            return Path.GetFullPath(envVar);
        }

        var configValue = configuration?[ConfigurationKeyName];
        if (!string.IsNullOrWhiteSpace(configValue))
        {
            // Support tilde expansion for home directory
            if (configValue.StartsWith("~/") || configValue.StartsWith("~\\"))
            {
                var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                configValue = Path.Combine(homeDirectory, configValue[2..]);
            }
            return Path.GetFullPath(configValue);
        }

        // Default to ~/.aspire/temp
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, DefaultSubdirectory);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
