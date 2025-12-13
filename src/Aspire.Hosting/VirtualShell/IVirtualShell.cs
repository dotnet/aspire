// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Provides a portable, mockable interface for executing CLI commands without invoking a shell.
/// Uses direct process execution with canonical parsing and PATH resolution.
/// </summary>
public interface IVirtualShell
{
    /// <summary>
    /// Creates a new shell with the specified working directory.
    /// </summary>
    /// <param name="workingDirectory">The working directory for commands.</param>
    /// <returns>A new shell instance with the specified working directory.</returns>
    IVirtualShell Cd(string workingDirectory);

    /// <summary>
    /// Creates a new shell with the specified environment variable set or removed.
    /// </summary>
    /// <param name="key">The environment variable name.</param>
    /// <param name="value">The value, or null to remove the variable.</param>
    /// <returns>A new shell instance with the updated environment.</returns>
    IVirtualShell Env(string key, string? value);

    /// <summary>
    /// Creates a new shell with the specified environment variables merged.
    /// </summary>
    /// <param name="vars">The environment variables to merge.</param>
    /// <returns>A new shell instance with the updated environment.</returns>
    IVirtualShell Env(IReadOnlyDictionary<string, string?> vars);

    /// <summary>
    /// Creates a new shell with the specified default timeout for commands.
    /// </summary>
    /// <param name="timeout">The default timeout for commands.</param>
    /// <returns>A new shell instance with the specified timeout.</returns>
    IVirtualShell Timeout(TimeSpan timeout);

    /// <summary>
    /// Creates a new shell with a diagnostic tag for categorizing operations.
    /// </summary>
    /// <param name="category">The category tag (e.g., "build", "deploy").</param>
    /// <returns>A new shell instance with the specified tag.</returns>
    IVirtualShell Tag(string category);

    /// <summary>
    /// Executes a command and waits for it to complete.
    /// </summary>
    /// <param name="commandLine">The command line to execute.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CliResult> Run(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command with explicit arguments and waits for it to complete.
    /// </summary>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CliResult> Run(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command and captures stdout (similar to shell $(...)).
    /// </summary>
    /// <param name="commandLine">The command line to execute.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The trimmed stdout of the command.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command fails (non-zero exit code).
    /// </exception>
    Task<string> Cap(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command with explicit arguments and captures stdout.
    /// </summary>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The trimmed stdout of the command.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command fails (non-zero exit code).
    /// </exception>
    Task<string> Cap(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command and streams output lines.
    /// Output is not captured by default for streaming methods.
    /// </summary>
    /// <param name="commandLine">The command line to execute.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    IAsyncEnumerable<OutputLine> Lines(
        string commandLine,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command with explicit arguments and streams output lines.
    /// </summary>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    IAsyncEnumerable<OutputLine> Lines(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null,
        CancellationToken ct = default);

    /// <summary>
    /// Starts a command and returns an advanced handle for streaming, stdin, and control.
    /// </summary>
    /// <param name="commandLine">The command line to execute.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    IStreamRun Stream(
        string commandLine,
        Action<ExecSpec>? perCall = null);

    /// <summary>
    /// Starts a command with explicit arguments and returns an advanced handle.
    /// </summary>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="perCall">Optional per-call configuration.</param>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    IStreamRun Stream(
        string fileName,
        IReadOnlyList<string> args,
        Action<ExecSpec>? perCall = null);
}
