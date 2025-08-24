// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Service responsible for launching processes with the correct .NET runtime.
/// </summary>
internal interface IProcessLauncher
{
    /// <summary>
    /// Launches a dotnet process with the specified arguments.
    /// </summary>
    /// <param name="arguments">Arguments to pass to dotnet.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="environmentVariables">Additional environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code of the process.</returns>
    Task<int> LaunchDotNetAsync(
        string arguments,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Launches a generic process with the specified executable and arguments.
    /// </summary>
    /// <param name="executablePath">Path to the executable.</param>
    /// <param name="arguments">Arguments to pass to the executable.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="environmentVariables">Additional environment variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code of the process.</returns>
    Task<int> LaunchAsync(
        string executablePath,
        string? arguments = null,
        string? workingDirectory = null,
        IDictionary<string, string>? environmentVariables = null,
        CancellationToken cancellationToken = default);
}