// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Service responsible for selecting and managing the .NET runtime to use.
/// </summary>
internal interface IDotNetRuntimeSelector
{
    /// <summary>
    /// Gets the path to the dotnet executable to use, installing the private SDK if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the dotnet executable.</returns>
    Task<string> GetDotNetExecutablePathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the mode being used (private, system, or custom).
    /// </summary>
    DotNetRuntimeMode Mode { get; }

    /// <summary>
    /// Initializes the runtime selector, potentially installing a private SDK if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets environment variables that should be set when launching processes, installing the private SDK if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of environment variables.</returns>
    Task<IDictionary<string, string>> GetEnvironmentVariablesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the mode of .NET runtime selection.
/// </summary>
internal enum DotNetRuntimeMode
{
    /// <summary>
    /// Use the system-installed .NET SDK.
    /// </summary>
    System,

    /// <summary>
    /// Use a private .NET SDK installed under ~/.aspire/sdk.
    /// </summary>
    Private,

    /// <summary>
    /// Use a custom path specified by the user.
    /// </summary>
    Custom
}