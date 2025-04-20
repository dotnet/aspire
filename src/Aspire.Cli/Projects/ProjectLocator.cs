// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Projects;

internal interface IProjectLocator
{
    FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile);
}

internal sealed class ProjectLocator(ILogger<ProjectLocator> logger, string currentDirectory) : IProjectLocator
{
    public FileInfo? UseOrFindAppHostProjectFile(FileInfo? projectFile)
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

        logger.LogDebug("No project file specified, searching for *.csproj files in {CurrentDirectory}", currentDirectory);
        var projectFilePaths = Directory.GetFiles(currentDirectory, "*.csproj");

        logger.LogDebug("Found {ProjectFileCount} project files.", projectFilePaths.Length);

        return projectFilePaths switch {
            { Length: 0 } => throw new ProjectLocatorException("No project file found."),
            { Length: > 1 } => throw new ProjectLocatorException("Multiple project files found."),
            { Length: 1 } => new FileInfo(projectFilePaths[0]),
        };
    }
}

internal class ProjectLocatorException : System.Exception
{
    public ProjectLocatorException(string message) : base(message) { }
}