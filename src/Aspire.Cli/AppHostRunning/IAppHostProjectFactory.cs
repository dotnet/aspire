// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for getting AppHost projects based on the project type.
/// </summary>
internal interface IAppHostProjectFactory
{
    /// <summary>
    /// Gets an AppHost project handler for the specified AppHost type.
    /// </summary>
    /// <param name="type">The type of AppHost.</param>
    /// <returns>An appropriate project handler for the AppHost type.</returns>
    /// <exception cref="NotSupportedException">Thrown if no handler is available for the specified type.</exception>
    IAppHostProject GetProject(AppHostType type);

    /// <summary>
    /// Tries to get a project handler for the specified AppHost file by detecting its type.
    /// </summary>
    /// <param name="appHostFile">The AppHost file.</param>
    /// <returns>An appropriate project handler, or null if the file type is not recognized.</returns>
    IAppHostProject? TryGetProject(FileInfo appHostFile);
}
