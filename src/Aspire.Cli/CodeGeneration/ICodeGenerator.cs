// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Service for generating SDK code from Aspire packages for a specific language.
/// </summary>
internal interface ICodeGenerator
{
    /// <summary>
    /// Gets the AppHost type that this generator supports.
    /// </summary>
    AppHostType SupportedType { get; }

    /// <summary>
    /// Generates SDK code for the specified app path.
    /// </summary>
    /// <param name="appPath">The path to the app.</param>
    /// <param name="packages">The Aspire packages to generate code for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the generation operation.</returns>
    Task GenerateAsync(
        string appPath,
        IEnumerable<(string PackageId, string Version)> packages,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if code generation is needed based on the current state.
    /// </summary>
    /// <param name="appPath">The path to the app.</param>
    /// <param name="packages">The packages to check.</param>
    /// <returns>True if generation is needed, false otherwise.</returns>
    bool NeedsGeneration(string appPath, IEnumerable<(string PackageId, string Version)> packages);
}
