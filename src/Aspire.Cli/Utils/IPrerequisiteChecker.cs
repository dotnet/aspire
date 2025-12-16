// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Interface for checking Aspire prerequisites and environment setup.
/// </summary>
internal interface IPrerequisiteChecker
{
    /// <summary>
    /// Checks if the .NET SDK is installed and meets the minimum version requirement.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the check result.</returns>
    Task<PrerequisiteCheckResult> CheckDotNetSdkAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a container runtime (Docker or Podman) is available and running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the check result.</returns>
    Task<PrerequisiteCheckResult> CheckContainerRuntimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if running in WSL environment and detects potential issues.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the check result.</returns>
    Task<PrerequisiteCheckResult> CheckWslEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Docker Engine (vs Docker Desktop) is installed and provides tunnel guidance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the check result.</returns>
    Task<PrerequisiteCheckResult> CheckDockerEngineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the terminal and environment capabilities (ANSI support, interactivity).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the check result.</returns>
    Task<PrerequisiteCheckResult> CheckTerminalCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs all prerequisite checks in order (fast checks first, expensive checks later).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns all check results.</returns>
    Task<IReadOnlyList<PrerequisiteCheckResult>> CheckAllAsync(CancellationToken cancellationToken = default);
}
