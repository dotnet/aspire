// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Projects;

/// <summary>
/// Factory for getting AppHost projects based on the file being handled.
/// </summary>
internal interface IAppHostProjectFactory
{
    /// <summary>
    /// Gets an AppHost project handler for the specified file.
    /// </summary>
    /// <param name="appHostFile">The AppHost file.</param>
    /// <returns>An appropriate project handler for the file.</returns>
    /// <exception cref="NotSupportedException">Thrown if no handler is available for the specified file.</exception>
    IAppHostProject GetProject(FileInfo appHostFile);

    /// <summary>
    /// Tries to get a project handler for the specified AppHost file.
    /// </summary>
    /// <param name="appHostFile">The AppHost file.</param>
    /// <returns>An appropriate project handler, or null if the file is not recognized.</returns>
    IAppHostProject? TryGetProject(FileInfo appHostFile);

    /// <summary>
    /// Gets a project handler by its language identifier.
    /// </summary>
    /// <param name="languageId">The language identifier (e.g., "csharp", "typescript").</param>
    /// <returns>The project handler for the language, or null if not found.</returns>
    IAppHostProject? GetProjectByLanguageId(string languageId);

    /// <summary>
    /// Gets all registered AppHost project handlers.
    /// </summary>
    /// <returns>All registered project handlers.</returns>
    IEnumerable<IAppHostProject> GetAllProjects();
}
