// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for getting AppHost projects based on the project type.
/// </summary>
internal sealed class AppHostProjectFactory : IAppHostProjectFactory
{
    private readonly IEnumerable<IAppHostProject> _projects;

    public AppHostProjectFactory(IEnumerable<IAppHostProject> projects)
    {
        _projects = projects;
    }

    /// <inheritdoc />
    public IAppHostProject GetProject(AppHostType type)
    {
        var project = _projects.FirstOrDefault(p => p.SupportedType == type);

        if (project is null)
        {
            throw new NotSupportedException($"No handler available for AppHost type '{type}'.");
        }

        return project;
    }

    /// <inheritdoc />
    public IAppHostProject? TryGetProject(FileInfo appHostFile)
    {
        var type = DetectAppHostType(appHostFile);

        if (type is null)
        {
            return null;
        }

        return _projects.FirstOrDefault(p => p.SupportedType == type.Value);
    }

    /// <summary>
    /// Detects the AppHost type from the file.
    /// </summary>
    private static AppHostType? DetectAppHostType(FileInfo appHostFile)
    {
        var extension = appHostFile.Extension.ToLowerInvariant();

        return extension switch
        {
            ".csproj" or ".fsproj" or ".vbproj" => AppHostType.DotNetProject,
            ".cs" => AppHostType.DotNetSingleFile,
            ".ts" => AppHostType.TypeScript,
            ".json" when appHostFile.Name.Equals("aspire.json", StringComparison.OrdinalIgnoreCase) => DetectFromAspireJson(appHostFile),
            _ => null
        };
    }

    /// <summary>
    /// Detects the AppHost type from an aspire.json file.
    /// </summary>
    private static AppHostType? DetectFromAspireJson(FileInfo aspireJsonFile)
    {
        // Look for sibling apphost.ts file
        var directory = aspireJsonFile.Directory;
        if (directory is null)
        {
            return null;
        }

        if (File.Exists(Path.Combine(directory.FullName, "apphost.ts")))
        {
            return AppHostType.TypeScript;
        }

        // TODO: Parse aspire.json to check for language field
        return null;
    }
}
