// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli.AppHostRunning;

/// <summary>
/// Defines the contract for running an AppHost.
/// </summary>
internal interface IAppHostRunner
{
    /// <summary>
    /// Runs the AppHost asynchronously.
    /// </summary>
    /// <param name="parseResult">The parsed command line arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code from running the AppHost.</returns>
    Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken);
}
