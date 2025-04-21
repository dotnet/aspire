// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface IProjectLocator
{
    Task<FileInfo?> UseOrFindAppHostProjectFileAsync(FileInfo? projectFile, CancellationToken cancellationToken = default);
}

internal sealed class ProjectLocator(ILogger<ProjectLocator> logger, IDotNetCliRunner runner, DirectoryInfo currentDirectory) : IProjectLocator
{
    private readonly ActivitySource _activitySource = new(nameof(ProjectLocator));

    private async Task<List<FileInfo>> FindAppHostProjectFilesAsync(DirectoryInfo searchDirectory, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var appHostProjects = new List<FileInfo>();

        logger.LogDebug("Searching for project files in {SearchDirectory}", searchDirectory.FullName);
        var projectFiles = searchDirectory.GetFiles("*.csproj", SearchOption.AllDirectories);
        logger.LogDebug("Found {ProjectFileCount} project files in {SearchDirectory}", projectFiles.Length, searchDirectory.FullName);

        foreach (var projectFile in projectFiles)
        {
            logger.LogDebug("Checking project file {ProjectFile}", projectFile.FullName);
            var information = await runner.GetAppHostInformationAsync(projectFile, cancellationToken);

            if (information.ExitCode == 0 && information.IsAspireHost)
            {
                logger.LogDebug("Found AppHost project file {ProjectFile} in {SearchDirectory}", projectFile.FullName, searchDirectory.FullName);
                appHostProjects.Add(projectFile);
            }
            else
            {
                logger.LogTrace("Project file {ProjectFile} in {SearchDirectory} is not an Aspire host", projectFile.FullName, searchDirectory.FullName);
            }
        }

        return appHostProjects;
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

                if (json.RootElement.TryGetProperty("appHostPath", out var appHostPathProperty) && appHostPathProperty.GetString() is { } appHostPath )
                {
                    
                    var qualifiedAppHostPath = Path.IsPathRooted(appHostPath) ? appHostPath : Path.Combine(settingsFile.Directory!.FullName, appHostPath);
                    var appHostFile = new FileInfo(qualifiedAppHostPath);

                    if (appHostFile.Exists)
                    {
                        return appHostFile;
                    }
                    else
                    {
                        throw new ProjectLocatorException($"AppHost file was specified in '{settingsFile.FullName}' but it does not exist.");
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

        logger.LogDebug("Found {ProjectFileCount} project files.", appHostProjects.Count);

        return appHostProjects.Count switch {
            0 => throw new ProjectLocatorException("No project file found."),
            > 1 => throw new ProjectLocatorException("Multiple project files found."),
            1 => appHostProjects[0],
            _ => throw new ProjectLocatorException("Unexpected number of project files found.")
        };
    }
}

internal class ProjectLocatorException : System.Exception
{
    public ProjectLocatorException(string message) : base(message) { }
}
