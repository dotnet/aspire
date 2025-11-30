// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Default implementation of <see cref="IDirectoryService"/>.
/// </summary>
internal sealed class DirectoryService : IDirectoryService
{
    private readonly TempDirectoryService _tempDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="DirectoryService"/>.
    /// </summary>
    /// <param name="configuration">Configuration to read settings from.</param>
    /// <param name="appHostName">Name of the AppHost project.</param>
    /// <param name="appHostSha">SHA256 hash of the AppHost path for uniqueness.</param>
    public DirectoryService(IConfiguration? configuration, string appHostName, string appHostSha)
    {
        _tempDirectory = new TempDirectoryService(configuration, appHostName, appHostSha);
    }

    /// <inheritdoc/>
    public ITempDirectoryService TempDirectory => _tempDirectory;

    /// <summary>
    /// Implementation of <see cref="ITempDirectoryService"/>.
    /// </summary>
    private sealed class TempDirectoryService : ITempDirectoryService
    {
        private const string ConfigurationKeyName = "Aspire:TempDirectory";
        private const string DefaultSubdirectory = ".aspire/temp";

        private readonly string _basePath;
        private readonly object _lock = new();

        public TempDirectoryService(IConfiguration? configuration, string appHostName, string appHostSha)
        {
            // Get the base temp root (e.g., ~/.aspire/temp)
            var tempRoot = ResolveTempRoot(configuration);

            // Create AppHost-specific subdirectory using name and SHA (lowercase for consistency)
            // Format: {tempRoot}/{apphostname}-{first12chars-of-sha}
            var appHostSubfolder = $"{SanitizeDirectoryName(appHostName).ToLowerInvariant()}-{appHostSha[..12].ToLowerInvariant()}";
            _basePath = Path.Combine(tempRoot, appHostSubfolder);

            EnsureDirectoryExists(_basePath);
        }

        /// <inheritdoc/>
        public string BasePath => _basePath;

        /// <inheritdoc/>
        public string CreateSubdirectory(string? prefix = null)
        {
            lock (_lock)
            {
                var subdirectoryName = string.IsNullOrEmpty(prefix)
                    ? Guid.NewGuid().ToString("N")
                    : $"{prefix}-{Guid.NewGuid():N}";

                var subdirectoryPath = Path.Combine(_basePath, subdirectoryName);
                EnsureDirectoryExists(subdirectoryPath);
                return subdirectoryPath;
            }
        }

        /// <inheritdoc/>
        public string GetFilePath(string? extension = null)
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

                return Path.Combine(_basePath, fileName);
            }
        }

        /// <inheritdoc/>
        public string CreateSubdirectoryPath(string subdirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subdirectory);
            var path = Path.Combine(_basePath, subdirectory);
            EnsureDirectoryExists(path);
            return path;
        }

        private static string ResolveTempRoot(IConfiguration? configuration)
        {
            // Priority is determined by IConfiguration source ordering
            // Typically: Command line > Environment variables > appsettings.json
            //
            // Check for Aspire:TempDirectory first (hierarchical key)
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

            // Also check ASPIRE_TEMP_FOLDER for convenience (flat key, common in environment variables)
            // This allows setting just ASPIRE_TEMP_FOLDER without the hierarchical separator
            var tempFolderValue = configuration?["ASPIRE_TEMP_FOLDER"];
            if (!string.IsNullOrWhiteSpace(tempFolderValue))
            {
                return Path.GetFullPath(tempFolderValue);
            }

            // Default to ~/.aspire/temp
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, DefaultSubdirectory);
        }

        private static string SanitizeDirectoryName(string name)
        {
            // Remove invalid path characters from the AppHost name
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("", name.Select(c => invalidChars.Contains(c) ? '-' : c));
            return sanitized;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
