// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Factory for creating AppHost runners based on the project type.
/// </summary>
internal interface IAppHostRunnerFactory
{
    /// <summary>
    /// Gets a runner for the specified AppHost type.
    /// </summary>
    /// <param name="type">The type of AppHost.</param>
    /// <returns>An appropriate runner for the AppHost type.</returns>
    /// <exception cref="NotSupportedException">Thrown if no runner is available for the specified type.</exception>
    IAppHostRunner GetRunner(AppHostType type);

    /// <summary>
    /// Tries to get a runner for the specified AppHost file by detecting its type.
    /// </summary>
    /// <param name="appHostFile">The AppHost file.</param>
    /// <returns>An appropriate runner, or null if the file type is not recognized.</returns>
    IAppHostRunner? TryGetRunner(FileInfo appHostFile);
}
