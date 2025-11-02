// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;

namespace Aspire.Cli;

internal sealed class CliExecutionContext(DirectoryInfo workingDirectory, DirectoryInfo hivesDirectory, DirectoryInfo cacheDirectory, DirectoryInfo sdksDirectory, bool debugMode = false)
{
    public DirectoryInfo WorkingDirectory { get; } = workingDirectory;
    public DirectoryInfo HivesDirectory { get; } = hivesDirectory;
    public DirectoryInfo CacheDirectory { get; } = cacheDirectory;
    public DirectoryInfo SdksDirectory { get; } = sdksDirectory;
    public bool DebugMode { get; } = debugMode;

    private BaseCommand? _command;

    /// <summary>
    /// Gets or sets the currently executing command. Setting this property also signals the CommandSelected task.
    /// </summary>
    public BaseCommand? Command
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
    public TaskCompletionSource<BaseCommand> CommandSelected { get; } = new();

    /// <summary>
    /// Determines whether the CLI is running as a dotnet tool.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the CLI is running as a dotnet tool; otherwise, <c>false</c> if running as a native binary.
    /// </returns>
    public static bool IsRunningAsDotNetTool()
    {
        // When running as a dotnet tool, the process path points to "dotnet" or "dotnet.exe"
        // When running as a native binary, it points to "aspire" or "aspire.exe"
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(processPath);
        return string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase);
    }
}