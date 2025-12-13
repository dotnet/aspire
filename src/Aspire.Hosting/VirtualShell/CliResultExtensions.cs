// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Extension methods for <see cref="CliResult"/>.
/// </summary>
public static class CliResultExtensions
{
    /// <summary>
    /// Logs the stdout and stderr of the result to the provided logger.
    /// </summary>
    /// <param name="result">The CLI result.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="commandName">A descriptive name for the command (e.g., "docker", "dotnet").</param>
    public static void LogOutput(this CliResult result, ILogger logger, string commandName)
    {
        if (!string.IsNullOrEmpty(result.Stdout))
        {
            logger.LogDebug("{Command} (stdout): {Output}", commandName, result.Stdout);
        }
        if (!string.IsNullOrEmpty(result.Stderr))
        {
            logger.LogDebug("{Command} (stderr): {Error}", commandName, result.Stderr);
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the result indicates failure.
    /// </summary>
    /// <param name="result">The CLI result.</param>
    /// <param name="errorMessage">A descriptive error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when the result indicates failure.</exception>
    public static void ThrowIfFailed(this CliResult result, string errorMessage)
    {
        if (!result.Success)
        {
            var message = $"{errorMessage} (exit code: {result.ExitCode})";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
    }
}
