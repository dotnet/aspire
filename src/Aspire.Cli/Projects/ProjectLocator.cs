// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface IProjectLocator
{
    Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default);
}

internal sealed class ProjectLocator(ILogger<ProjectLocator> logger, IDotNetCliRunner runner, DirectoryInfo currentDirectory, IInteractionService interactionService, IConfigurationService configurationService, AspireCliActivityTelemetry telemetry) : IProjectLocator
{

    private async Task<List<FileInfo>> FindAppHostProjectFilesAsync(DirectoryInfo searchDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        return await interactionService.ShowStatusAsync("Searching", async () =>
        {
            var appHostProjects = new List<FileInfo>();
            var lockObject = new object();
            logger.LogDebug("Searching for project files in {SearchDirectory}", searchDirectory.FullName);
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            interactionService.DisplayMessage("magnifying_glass_tilted_left", "Finding app hosts...");
            var projectFiles = searchDirectory.GetFiles("*.csproj", enumerationOptions);
            logger.LogDebug("Found {ProjectFileCount} project files in {SearchDirectory}", projectFiles.Length, searchDirectory.FullName);

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(projectFiles, parallelOptions, async (projectFile, ct) =>
            {
                logger.LogDebug("Checking project file {ProjectFile}", projectFile.FullName);
                var information = await runner.GetAppHostInformationAsync(projectFile, new DotNetCliRunnerInvocationOptions(), ct);

                if (information.ExitCode == 0 && information.IsAspireHost)
                {
                    logger.LogDebug("Found AppHost project file {ProjectFile} in {SearchDirectory}", projectFile.FullName, searchDirectory.FullName);
                    var relativePath = Path.GetRelativePath(currentDirectory.FullName, projectFile.FullName);
                    interactionService.DisplaySubtleMessage(relativePath);
                    lock (lockObject)
                    {
                        appHostProjects.Add(projectFile);
                    }
                }
                else
                {
                    logger.LogTrace("Project file {ProjectFile} in {SearchDirectory} is not an Aspire host", projectFile.FullName, searchDirectory.FullName);
                }
            });

            // This sort is done here to make results deterministic since we get all the app
            // host information in parallel and the order may vary.
            appHostProjects.Sort((x, y) => x.FullName.CompareTo(y.FullName));

            return appHostProjects;
        });
    }

    private async Task<FileInfo?> GetAppHostProjectFileFromSettingsAsync(CancellationToken cancellationToken)
    {
        var searchDirectory = currentDirectory;

        while (true)
        {
            var settingsFile = new FileInfo(Path.Combine(searchDirectory.FullName, ".aspire", "settings.json"));

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
                        interactionService.DisplayMessage("warning", $"AppHost file was specified in '{settingsFile.FullName}' but it does not exist at '{qualifiedAppHostPath}'.");
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
        logger.LogDebug("Finding project file in {CurrentDirectory}", currentDirectory);

        if (projectFile is not null)
        {
            // If the project file is passed, just use it.
            if (!projectFile.Exists)
            {
                logger.LogError("Project file {ProjectFile} does not exist.", projectFile.FullName);
                throw new ProjectLocatorException($"Project file does not exist.");
            }

            logger.LogDebug("Using project file {ProjectFile}", projectFile.FullName);
            return projectFile;
        }

        projectFile = await GetAppHostProjectFileFromSettingsAsync(cancellationToken);

        if (projectFile is not null)
        {
            return projectFile;
        }

        logger.LogDebug("No project file specified, searching for *.csproj files in {CurrentDirectory}", currentDirectory);
        var appHostProjects = await FindAppHostProjectFilesAsync(currentDirectory, cancellationToken);
        interactionService.DisplayEmptyLine();

        logger.LogDebug("Found {ProjectFileCount} project files.", appHostProjects.Count);

        FileInfo? selectedAppHost = null;

        if (appHostProjects.Count == 0)
        {
            throw new ProjectLocatorException("No project file found.");
        }
        else if (appHostProjects.Count == 1)
        {
            selectedAppHost = appHostProjects[0];
        }
        else if (appHostProjects.Count > 1)
        {
            selectedAppHost = await interactionService.PromptForSelectionAsync(
                "Select app host to run",
                appHostProjects,
                projectFile => $"{projectFile.Name} ({Path.GetRelativePath(currentDirectory.FullName, projectFile.FullName)})",
                cancellationToken
                );
        }

        await CreateSettingsFileAsync(selectedAppHost!, cancellationToken);
        return selectedAppHost;
    }

    private async Task CreateSettingsFileAsync(FileInfo projectFile, CancellationToken cancellationToken)
    {
        var settingsFilePath = Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");
        var settingsFile = new FileInfo(settingsFilePath);

        logger.LogDebug("Creating settings file at {SettingsFilePath}", settingsFile.FullName);

        var relativePathToProjectFile = Path.GetRelativePath(settingsFile.Directory!.FullName, projectFile.FullName).Replace(Path.DirectorySeparatorChar, '/');

        // Use the configuration writer to set the appHostPath, which will merge with any existing settings
        await configurationService.SetConfigurationAsync("appHostPath", relativePathToProjectFile, isGlobal: false, cancellationToken);
        
        var relativeSettingsFilePath = Path.GetRelativePath(currentDirectory.FullName, settingsFile.FullName).Replace(Path.DirectorySeparatorChar, '/');
        interactionService.DisplayMessage("file_cabinet", $"Created settings file at [bold]'{relativeSettingsFilePath}'[/].");
    }
}

internal class ProjectLocatorException : System.Exception
{
    public ProjectLocatorException(string message) : base(message) { }
}
