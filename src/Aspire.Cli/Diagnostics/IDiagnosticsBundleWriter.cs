// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Diagnostics;

/// <summary>
/// Service for writing diagnostic bundles when CLI commands fail.
/// </summary>
internal interface IDiagnosticsBundleWriter
{
    /// <summary>
    /// Writes a failure bundle to disk with diagnostic information.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="exitCode">The exit code that will be returned.</param>
    /// <param name="commandName">The name of the command that failed.</param>
    /// <param name="additionalContext">Optional additional context about the failure.</param>
    /// <returns>The path to the diagnostics bundle directory, or null if writing failed.</returns>
    Task<string?> WriteFailureBundleAsync(
        Exception exception, 
        int exitCode, 
        string commandName, 
        string? additionalContext = null);
}
