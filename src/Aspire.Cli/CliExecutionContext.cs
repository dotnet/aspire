// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Aspire.Cli;

internal sealed class CliExecutionContext(DirectoryInfo workingDirectory, DirectoryInfo hivesDirectory, DirectoryInfo cacheDirectory, DirectoryInfo sdksDirectory, bool debugMode = false, IReadOnlyDictionary<string, string?>? environmentVariables = null, DirectoryInfo? homeDirectory = null)
{
    public DirectoryInfo WorkingDirectory { get; } = workingDirectory;
    public DirectoryInfo HivesDirectory { get; } = hivesDirectory;
    public DirectoryInfo CacheDirectory { get; } = cacheDirectory;
    public DirectoryInfo SdksDirectory { get; } = sdksDirectory;
    public DirectoryInfo HomeDirectory { get; } = homeDirectory ?? new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    public bool DebugMode { get; } = debugMode;

    /// <summary>
    /// Gets the environment variables for the CLI execution context.
    /// If null, the process environment variables should be used.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? EnvironmentVariables { get; } = environmentVariables;

    /// <summary>
    /// Gets an environment variable value. Checks the context's environment variables first,
    /// then falls back to the process environment if no custom environment was provided.
    /// When a custom environment dictionary is provided (even if empty), only that dictionary is used
    /// and no fallback to the process environment occurs.
    /// </summary>
    /// <param name="variable">The environment variable name.</param>
    /// <returns>The value of the environment variable, or null if not found.</returns>
    public string? GetEnvironmentVariable(string variable)
    {
        if (EnvironmentVariables is not null)
        {
            // If a custom environment dictionary was provided, only use it (don't fall back)
            return EnvironmentVariables.TryGetValue(variable, out var value) ? value : null;
        }

        return Environment.GetEnvironmentVariable(variable);
    }

    private Command? _command;

    /// <summary>
    /// Gets or sets the currently executing command. Setting this property also signals the CommandSelected task.
    /// </summary>
    public Command? Command
    {
        get => _command;
        set
        {
            _command = value;
            if (value is not null)
            {
                CommandSelected.TrySetResult(value);
            }
        }
    }

    /// <summary>
    /// TaskCompletionSource that is completed when a command is selected and set on this context.
    /// </summary>
    public TaskCompletionSource<Command> CommandSelected { get; } = new();

    /// <summary>
    /// Gets a value indicating whether hives (PR build directories) exist on the developer machine.
    /// Hives are detected when the hives directory exists and contains subdirectories.
    /// </summary>
    public bool HasHives => HivesDirectory.Exists && HivesDirectory.GetDirectories().Length > 0;
}