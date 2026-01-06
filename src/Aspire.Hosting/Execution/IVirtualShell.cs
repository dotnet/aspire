// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides a portable, mockable interface for executing CLI commands without invoking a shell.
/// Uses direct process execution with canonical parsing and PATH resolution.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
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
    /// Creates a new shell with the specified path prepended to the PATH environment variable.
    /// </summary>
    /// <param name="path">The path to prepend.</param>
    /// <returns>A new shell instance with the updated PATH.</returns>
    IVirtualShell PrependPath(string path);

    /// <summary>
    /// Creates a new shell with the specified path appended to the PATH environment variable.
    /// </summary>
    /// <param name="path">The path to append.</param>
    /// <returns>A new shell instance with the updated PATH.</returns>
    IVirtualShell AppendPath(string path);

    /// <summary>
    /// Defines a named secret value that will be redacted in logs and traces.
    /// </summary>
    /// <param name="name">The name to reference this secret by.</param>
    /// <param name="value">The secret value.</param>
    /// <returns>A new shell instance with the secret registered.</returns>
    IVirtualShell DefineSecret(string name, string value);

    /// <summary>
    /// Gets the value of a previously defined secret by name.
    /// The returned value will be redacted in logs and traces.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <returns>The secret value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the secret name is not defined.</exception>
    string Secret(string name);

    /// <summary>
    /// Creates a new shell with an environment variable set to a secret value.
    /// The value will be redacted in logs and traces.
    /// </summary>
    /// <param name="key">The environment variable name.</param>
    /// <param name="value">The secret value.</param>
    /// <returns>A new shell instance with the secret environment variable set.</returns>
    IVirtualShell SecretEnv(string key, string value);

    /// <summary>
    /// Creates a new shell with a diagnostic tag for categorizing operations.
    /// </summary>
    /// <param name="category">The category tag (e.g., "build", "deploy").</param>
    /// <returns>A new shell instance with the specified tag.</returns>
    IVirtualShell Tag(string category);

    /// <summary>
    /// Creates a new shell with logging enabled for command execution.
    /// When enabled, commands will emit structured logs for start, completion, and failure.
    /// </summary>
    /// <returns>A new shell instance with logging enabled.</returns>
    IVirtualShell WithLogging();

    /// <summary>
    /// Creates a command from a command line string.
    /// The string is parsed to extract the executable and arguments.
    /// </summary>
    /// <param name="commandLine">The command line to parse (e.g., "dotnet build --configuration Release").</param>
    /// <returns>A command ready for execution.</returns>
    ICommand Command(string commandLine);

    /// <summary>
    /// Creates a command with an explicit executable and arguments.
    /// </summary>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <returns>A command ready for execution.</returns>
    ICommand Command(string fileName, IReadOnlyList<string>? args);
}

/// <summary>
/// Extension methods for <see cref="IVirtualShell"/> providing convenient one-shot execution shortcuts.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class VirtualShellExtensions
{
    /// <summary>
    /// Runs a command to completion and returns the result.
    /// This is a shortcut for <c>Command(commandLine).RunAsync(ct: ct)</c>.
    /// </summary>
    /// <param name="shell">The shell instance.</param>
    /// <param name="commandLine">The command line to parse and execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The process result with exit code and captured output.</returns>
    public static Task<ProcessResult> RunAsync(this IVirtualShell shell, string commandLine, CancellationToken ct = default)
    {
        return shell.Command(commandLine).RunAsync(ct: ct);
    }

    /// <summary>
    /// Runs a command to completion and returns the result.
    /// This is a shortcut for <c>Command(fileName, args).RunAsync(ct: ct)</c>.
    /// </summary>
    /// <param name="shell">The shell instance.</param>
    /// <param name="fileName">The executable name or path.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The process result with exit code and captured output.</returns>
    public static Task<ProcessResult> RunAsync(this IVirtualShell shell, string fileName, IReadOnlyList<string>? args, CancellationToken ct = default)
    {
        return shell.Command(fileName, args).RunAsync(ct: ct);
    }
}
