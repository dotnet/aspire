// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.DotNet;

/// <summary>
/// Service responsible for selecting and managing the .NET runtime to use.
/// </summary>
internal interface IDotNetRuntimeSelector
{
    /// <summary>
    /// Initializes the runtime selector, potentially installing a private SDK if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);
}