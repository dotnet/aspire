// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Processes;

namespace Aspire.Cli.Utils;

/// <summary>
/// Handles automatic background updates of the Aspire CLI.
/// </summary>
internal static class AutoUpdater
{
    private const string StagingDirectoryName = "staging";
    private const string VersionFileName = "version.txt";
    private const int DownloadTimeoutSeconds = 600;
    private const int ChecksumTimeoutSeconds = 120;

    /// <summary>
    /// Set when a staged update was applied at startup.
    /// Read by BaseCommand to show the auto-update notification.
    /// </summary>
    public static string? AppliedVersion { get; set; }

    /// <summary>
    /// Gets the path to the staging directory (~/.aspire/staging/).
    /// </summary>
    public static string GetStagingDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".aspire", StagingDirectoryName);
    }

    /// <summary>
    /// Checks whether auto-update should run.
    /// </summary>
    public static bool ShouldAutoUpdate(
        ICliHostEnvironment hostEnvironment,
        IFeatures features,
        CliExecutionContext executionContext)
    {
        // Skip if feature disabled via 'aspire config set features.autoUpdateEnabled false'
        if (!features.IsFeatureEnabled(KnownFeatures.AutoUpdateEnabled, true))
        {
            return false;
        }

        // Skip in CI environments
        if (hostEnvironment.IsRunningInCI)
        {
            return false;
        }

        // Skip if disabled via env var: ASPIRE_CLI_AUTO_UPDATE=false
        var autoUpdateEnv = executionContext.GetEnvironmentVariable("ASPIRE_CLI_AUTO_UPDATE");
        if (string.Equals(autoUpdateEnv, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip for dotnet tool installs (use 'dotnet tool update' instead)
        if (CliUpdateHelper.IsRunningAsDotNetTool())
        {
            return false;
        }

        // Skip if staging already has a pending update
        if (HasStagedUpdate())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Spawns a detached background process to download and stage the CLI update.
    /// </summary>
    public static void SpawnBackgroundDownload(string cliDownloadBaseUrl, string? version)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return;
        }

        try
        {
            var arguments = new List<string> { "internal-auto-update", cliDownloadBaseUrl };
            if (version is not null)
            {
                arguments.Add(version);
            }

            DetachedProcessLauncher.Start(processPath, arguments, Environment.CurrentDirectory);
        }
        catch
        {
            // Silently ignore spawn failures
        }
    }

    /// <summary>
    /// Downloads the CLI archive and stages it for the next startup.
    /// This runs in a standalone background process with no DI.
    /// </summary>
    public static async Task<int> SilentDownloadAndStageAsync(string baseUrl, string? version)
    {
        try
        {
            var (archiveUrl, checksumUrl, archiveFilename) = CliUpdateHelper.GetDownloadUrls(baseUrl);
            var exeName = CliUpdateHelper.ExeName;

            var stagingDir = GetStagingDirectory();

            // If staging already has an update, skip
            if (File.Exists(Path.Combine(stagingDir, exeName)))
            {
                return 0;
            }

            var tempDir = Directory.CreateTempSubdirectory("aspire-auto-update").FullName;

            try
            {
                var archivePath = Path.Combine(tempDir, archiveFilename);
                var checksumPath = Path.Combine(tempDir, $"{archiveFilename}.sha512");

                // Download archive and checksum
                using var httpClient = new HttpClient();

                await CliUpdateHelper.DownloadFileAsync(httpClient, archiveUrl, archivePath, DownloadTimeoutSeconds);
                await CliUpdateHelper.DownloadFileAsync(httpClient, checksumUrl, checksumPath, ChecksumTimeoutSeconds);

                // Validate checksum
                await CliUpdateHelper.ValidateChecksumAsync(archivePath, checksumPath);

                // Extract archive
                var extractDir = Path.Combine(tempDir, "extracted");
                await ArchiveHelper.ExtractAsync(archivePath, extractDir, CancellationToken.None);

                // Find the CLI exe in extracted files
                var newExePath = Path.Combine(extractDir, exeName);
                if (!File.Exists(newExePath))
                {
                    return 1;
                }

                // Stage the new binary atomically: write to temp file, then rename
                // This prevents concurrent processes from reading a partially-written binary
                Directory.CreateDirectory(stagingDir);
                var tempStagedPath = Path.Combine(stagingDir, $"{exeName}.tmp.{Environment.ProcessId}");
                try
                {
                    File.Copy(newExePath, tempStagedPath, overwrite: true);
                    File.Move(tempStagedPath, Path.Combine(stagingDir, exeName), overwrite: true);
                }
                catch
                {
                    // Clean up temp file on failure
                    try { File.Delete(tempStagedPath); } catch { }
                    throw;
                }

                // Write version marker
                if (version is not null)
                {
                    await File.WriteAllTextAsync(
                        Path.Combine(stagingDir, VersionFileName),
                        version);
                }

                return 0;
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// Checks for and applies a staged update. Called very early in Program.Main before DI setup.
    /// Uses the rename-to-.old trick for Windows locked file handling.
    /// </summary>
    /// <returns>The version that was applied, or null if no update was staged.</returns>
    public static string? TryApplyStagedUpdate()
    {
        try
        {
            var stagingDir = GetStagingDirectory();
            var exeName = CliUpdateHelper.ExeName;
            var stagedExePath = Path.Combine(stagingDir, exeName);

            if (!File.Exists(stagedExePath))
            {
                return null;
            }

            var currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                return null;
            }

            // Guard: ensure we're updating the aspire executable, not the dotnet host.
            var currentExeFileName = Path.GetFileName(currentExePath);
            if (!string.Equals(currentExeFileName, exeName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Read version before we move files
            string? version = null;
            var versionFile = Path.Combine(stagingDir, VersionFileName);
            if (File.Exists(versionFile))
            {
                version = File.ReadAllText(versionFile).Trim();
            }

            try
            {
                CliUpdateHelper.ReplaceExecutable(currentExePath, stagedExePath);
            }
            catch
            {
                return null;
            }

            // Clean up staging directory
            try
            {
                Directory.Delete(stagingDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }

            CliUpdateHelper.CleanupOldBackupFiles(currentExePath);

            return version;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if there is a staged update ready to apply.
    /// </summary>
    public static bool HasStagedUpdate()
    {
        var stagingDir = GetStagingDirectory();
        return File.Exists(Path.Combine(stagingDir, CliUpdateHelper.ExeName));
    }
}
