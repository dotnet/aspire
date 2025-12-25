// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Interface for running AppHost projects of various types.
/// </summary>
internal interface IAppHostRunner
{
    /// <summary>
    /// Gets the AppHost type that this runner supports.
    /// </summary>
    AppHostType SupportedType { get; }

    /// <summary>
    /// Runs the AppHost project.
    /// </summary>
    /// <param name="context">The context containing all information needed to run the AppHost.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The exit code from running the AppHost.</returns>
    Task<int> RunAsync(AppHostRunnerContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that the AppHost file is compatible with this runner.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the AppHost is valid and compatible; otherwise, false.</returns>
    Task<bool> ValidateAsync(FileInfo appHostFile, CancellationToken cancellationToken);
}
