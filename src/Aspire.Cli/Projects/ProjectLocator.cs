// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface IProjectLocator
{
    Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default);
}

internal sealed class ProjectLocator(ILogger<ProjectLocator> logger, IDotNetCliRunner runner, CliExecutionContext executionContext, IInteractionService interactionService, IConfigurationService configurationService, AspireCliTelemetry telemetry) : IProjectLocator
{

    private async Task<(List<FileInfo> BuildableAppHost, List<FileInfo> UnbuildableSuspectedAppHostProjects)> FindAppHostProjectFilesAsync(DirectoryInfo searchDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        return await interactionService.ShowStatusAsync(InteractionServiceStrings.SearchingProjects, async () =>
        {
            var appHostProjects = new List<FileInfo>();
            var unbuildableSuspectedAppHostProjects = new List<FileInfo>();
            var lockObject = new object();
            logger.LogDebug("Searching for project files in {SearchDirectory}", searchDirectory.FullName);
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            interactionService.DisplayMessage("magnifying_glass_tilted_left", InteractionServiceStrings.FindingAppHosts);
            
            // Search for both .csproj files and apphost.cs files
            var projectFiles = searchDirectory.GetFiles("*.csproj", enumerationOptions);
            var singleFiles = searchDirectory.GetFiles("apphost.cs", enumerationOptions);
            var allFiles = projectFiles.Concat(singleFiles).ToArray();
            
            logger.LogDebug("Found {ProjectFileCount} project files and {SingleFileCount} single files in {SearchDirectory}", 
                projectFiles.Length, singleFiles.Length, searchDirectory.FullName);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(allFiles, parallelOptions, async (file, ct) =>
            {
                logger.LogDebug("Checking file {File}", file.FullName);
                
                bool isAspireHost = false;
                string? aspireHostingVersion = null;
                int exitCode = 0;
                
                if (file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle single C# file
                    var (isSingleFileAppHost, version) = await IsSingleFileAppHostAsync(file, ct);
                    isAspireHost = isSingleFileAppHost;
                    aspireHostingVersion = version;
                }
                else
                {
                    // Handle project file
                    var information = await runner.GetAppHostInformationAsync(file, new DotNetCliRunnerInvocationOptions(), ct);
                    exitCode = information.ExitCode;
                    isAspireHost = information.IsAspireHost;
                    aspireHostingVersion = information.AspireHostingVersion;
                }

                if (exitCode == 0 && isAspireHost)
                {
                    logger.LogDebug("Found AppHost file {File} in {SearchDirectory}", file.FullName, searchDirectory.FullName);
                    var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, file.FullName);
                    interactionService.DisplaySubtleMessage(relativePath);
                    lock (lockObject)
                    {
                        appHostProjects.Add(file);
                    }
                }
                else if (!file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) && IsPossiblyUnbuildableAppHost(file))
                {
                    var relativePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, file.FullName);
                    interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.ProjectFileMayBeUnbuildableAppHost, relativePath));
                    unbuildableSuspectedAppHostProjects.Add(file);
                }
                else
                {
                    logger.LogTrace("File {File} in {SearchDirectory} is not an Aspire host", file.FullName, searchDirectory.FullName);
                }
            });

            // This sort is done here to make results deterministic since we get all the app
            // host information in parallel and the order may vary.
            appHostProjects.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return (appHostProjects, unbuildableSuspectedAppHostProjects);
        });
    }

    private static bool IsPossiblyUnbuildableAppHost(FileInfo projectFile)
    {
        var fileNameSuggestsAppHost = () => projectFile.Name.EndsWith("AppHost.csproj", StringComparison.OrdinalIgnoreCase);
        var folderContainsAppHostCSharpFile = () => projectFile.Directory!.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(f => f.Name.Equals("AppHost.cs", StringComparison.OrdinalIgnoreCase));
        return fileNameSuggestsAppHost() || folderContainsAppHostCSharpFile();
    }

    private static async Task<(bool IsSingleFileAppHost, string? AspireVersion)> IsSingleFileAppHostAsync(FileInfo singleFile, CancellationToken cancellationToken)
    {
        try
        {
            // Read the first few lines of the file to check for the SDK directive
            var lines = await File.ReadAllLinesAsync(singleFile.FullName, cancellationToken);
            
            // Look for the SDK directive in the first few lines
            foreach (var line in lines.Take(5))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("#:sdk", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse the SDK directive to extract version
                    // Format: #:sdk Aspire.AppHost.Sdk@version
                    var parts = trimmedLine.Split('@');
                    if (parts.Length == 2 && parts[0].Contains("Aspire.AppHost.Sdk", StringComparison.OrdinalIgnoreCase))
                    {
                        var version = parts[1].Trim();
                        return (true, version);
                    }
                }
            }
            
            return (false, null);
        }
        catch (Exception)
        {
            // If we can't read the file, assume it's not an AppHost
            return (false, null);
        }
    }

    private async Task<FileInfo?> GetAppHostProjectFileFromSettingsAsync(CancellationToken cancellationToken)
    {
        var searchDirectory = executionContext.WorkingDirectory;

        while (true)
        {
            var settingsFile = new FileInfo(ConfigurationHelper.BuildPathToSettingsJsonFile(searchDirectory.FullName));

            if (settingsFile.Exists)
            {
                using var stream = settingsFile.OpenRead();
                var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (json.RootElement.TryGetProperty("appHostPath", out var appHostPathProperty) && appHostPathProperty.GetString() is { } appHostPath)
                {

                    var qualifiedAppHostPath = Path.IsPathRooted(appHostPath) ? appHostPath : Path.Combine(settingsFile.Directory!.FullName, appHostPath);
                    var appHostFile = new FileInfo(qualifiedAppHostPath);

                    if (appHostFile.Exists)
                    {
                        return appHostFile;
                    }
                    else
                    {
                        // AppHost file was specified but doesn't exist, return null to trigger fallback logic
                        interactionService.DisplayMessage("warning", string.Format(CultureInfo.CurrentCulture, ErrorStrings.AppHostWasSpecifiedButDoesntExist, settingsFile.FullName, qualifiedAppHostPath));
                        return null;
                    }
                }
            }

            if (searchDirectory.Parent is not null)
            {
                searchDirectory = searchDirectory.Parent;
            }
            else
            {
                return null;
            }
        }
    }

    public async Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding project file in {CurrentDirectory}", executionContext.WorkingDirectory);

        if (projectFile is not null)
        {
            // If the project file is passed, just use it.
            if (!projectFile.Exists)
            {
                logger.LogError("Project file {ProjectFile} does not exist.", projectFile.FullName);
                throw new ProjectLocatorException(ErrorStrings.ProjectFileDoesntExist);
            }

            logger.LogDebug("Using project file {ProjectFile}", projectFile.FullName);
            return projectFile;
        }

        projectFile = await GetAppHostProjectFileFromSettingsAsync(cancellationToken);

        if (projectFile is not null)
        {
            return projectFile;
        }

        logger.LogDebug("No project file specified, searching for *.csproj files in {CurrentDirectory}", executionContext.WorkingDirectory);
        var results = await FindAppHostProjectFilesAsync(executionContext.WorkingDirectory, cancellationToken);
        interactionService.DisplayEmptyLine();

        logger.LogDebug("Found {ProjectFileCount} project files.", results.BuildableAppHost.Count);

        FileInfo? selectedAppHost = null;

        if (results.BuildableAppHost.Count == 0 && results.UnbuildableSuspectedAppHostProjects.Count == 0)
        {
            throw new ProjectLocatorException(ErrorStrings.NoProjectFileFound);
        }
        else if (results.BuildableAppHost.Count == 0 && results.UnbuildableSuspectedAppHostProjects.Count > 0)
        {
            throw new ProjectLocatorException(ErrorStrings.AppHostsMayNotBeBuildable);
        }
        else if (results.BuildableAppHost.Count == 1)
        {
            selectedAppHost = results.BuildableAppHost[0];
        }
        else if (results.BuildableAppHost.Count > 1)
        {
            selectedAppHost = await interactionService.PromptForSelectionAsync(
                InteractionServiceStrings.SelectAppHostToUse,
                results.BuildableAppHost,
                projectFile => $"{projectFile.Name} ({Path.GetRelativePath(executionContext.WorkingDirectory.FullName, projectFile.FullName)})",
                cancellationToken
                );
        }

        await CreateSettingsFileAsync(selectedAppHost!, cancellationToken);
        return selectedAppHost;
    }

    private async Task CreateSettingsFileAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var settingsFilePath = ConfigurationHelper.BuildPathToSettingsJsonFile(executionContext.WorkingDirectory.FullName);
        var settingsFile = new FileInfo(settingsFilePath);

        logger.LogDebug("Creating settings file at {SettingsFilePath}", settingsFile.FullName);

        var relativePathToProjectFile = Path.GetRelativePath(settingsFile.Directory!.FullName, projectFile.FullName).Replace(Path.DirectorySeparatorChar, '/');

        // Use the configuration writer to set the appHostPath, which will merge with any existing settings
        await configurationService.SetConfigurationAsync("appHostPath", relativePathToProjectFile, isGlobal: false, cancellationToken);

        var relativeSettingsFilePath = Path.GetRelativePath(executionContext.WorkingDirectory.FullName, settingsFile.FullName).Replace(Path.DirectorySeparatorChar, '/');
        interactionService.DisplayMessage("file_cabinet", string.Format(CultureInfo.CurrentCulture, InteractionServiceStrings.CreatedSettingsFile, $"[bold]'{relativeSettingsFilePath}'[/]"));
    }
}

internal class ProjectLocatorException : System.Exception
{
    public ProjectLocatorException(string message) : base(message) { }
}
