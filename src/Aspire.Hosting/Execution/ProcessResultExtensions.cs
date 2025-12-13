// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Extension methods for <see cref="ProcessResult"/>.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class ProcessResultExtensions
{
    /// <summary>
    /// Logs the stdout and stderr of the result to the provided logger.
    /// </summary>
    /// <param name="result">The process result.</param>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="commandName">A descriptive name for the command (e.g., "docker", "dotnet").</param>
    public static void LogOutput(this ProcessResult result, ILogger logger, string commandName)
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
    /// <param name="result">The process result.</param>
    /// <param name="errorMessage">A descriptive error message.</param>
    /// <exception cref="InvalidOperationException">Thrown when the result indicates failure.</exception>
    public static void ThrowIfFailed(this ProcessResult result, string errorMessage)
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

    /// <summary>
    /// Ensures the command succeeded, throwing if it failed. Returns the result for chaining.
    /// </summary>
    /// <param name="result">The process result.</param>
    /// <param name="errorMessage">An optional error message. If not provided, a default message is used.</param>
    /// <returns>The result, for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result indicates failure.</exception>
    public static ProcessResult EnsureSuccess(this ProcessResult result, string? errorMessage = null)
    {
        if (!result.Success)
        {
            var message = errorMessage ?? $"Command failed with exit code {result.ExitCode}";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
        return result;
    }
}
