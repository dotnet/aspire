// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for getting AppHost projects based on the file being handled.
/// </summary>
internal sealed class AppHostProjectFactory : IAppHostProjectFactory
{
    private readonly IEnumerable<IAppHostProject> _projects;

    public AppHostProjectFactory(IEnumerable<IAppHostProject> projects)
    {
        _projects = projects;
    }

    /// <inheritdoc />
    public IAppHostProject GetProject(FileInfo appHostFile)
    {
        var project = _projects.FirstOrDefault(p => p.CanHandle(appHostFile));

        if (project is null)
        {
            throw new NotSupportedException($"No handler available for AppHost file '{appHostFile.Name}'.");
        }

        return project;
    }

    /// <inheritdoc />
    public IAppHostProject? TryGetProject(FileInfo appHostFile)
    {
        return _projects.FirstOrDefault(p => p.CanHandle(appHostFile));
    }

    /// <inheritdoc />
    public IAppHostProject? GetProjectByLanguageId(string languageId)
    {
        return _projects.FirstOrDefault(p => p.LanguageId.Equals(languageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public IEnumerable<IAppHostProject> GetAllProjects()
    {
        return _projects;
    }
}
