// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Dcp.Process;

/// <summary>
/// Provides factory methods to create a step that runs an executable process.
/// </summary>
[Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class ExecStep
{
    private readonly ProcessSpec _processSpec;

    private ExecStep(ProcessSpec processSpec)
    {
        _processSpec = processSpec ?? throw new ArgumentNullException(nameof(processSpec));
    }

    /// <summary>
    /// Gets the underlying <see cref="ProcessSpec"/> for this execution step.
    /// </summary>
    internal ProcessSpec ProcessSpec => _processSpec;

    /// <summary>
    /// Creates an execution step from a full command line string.
    /// </summary>
    /// <param name="commandLine">The full command line including the executable and arguments.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <returns>An <see cref="ExecStep"/> instance that can be used to execute the process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandLine"/> or <paramref name="workingDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="commandLine"/> is empty or whitespace.</exception>
    /// <remarks>
    /// The command line will be parsed to extract the executable path and arguments.
    /// </remarks>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static ExecStep Create(string commandLine, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var (executable, args) = ParseCommandLine(commandLine);

        var processSpec = new ProcessSpec(executable)
        {
            WorkingDirectory = workingDirectory,
            Arguments = args
        };

        return new ExecStep(processSpec);
    }

    /// <summary>
    /// Creates an execution step from an executable path and separate arguments.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <returns>An <see cref="ExecStep"/> instance that can be used to execute the process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/>, <paramref name="args"/>, or <paramref name="workingDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="executable"/> is empty or whitespace.</exception>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static ExecStep Create(string executable, string[] args, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var arguments = string.Join(" ", args.Select(EscapeArgument));

        var processSpec = new ProcessSpec(executable)
        {
            WorkingDirectory = workingDirectory,
            Arguments = arguments
        };

        return new ExecStep(processSpec);
    }

    /// <summary>
    /// Creates an execution step with the ability to customize the process start configuration.
    /// </summary>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="configure">A callback to customize the <see cref="ProcessStartInfo"/> before the process starts.</param>
    /// <returns>An <see cref="ExecStep"/> instance that can be used to execute the process.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="executable"/>, <paramref name="args"/>, <paramref name="workingDirectory"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="executable"/> is empty or whitespace.</exception>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static ExecStep Create(string executable, string[] args, string workingDirectory, Action<ProcessStartInfo> configure)
    {
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        ArgumentNullException.ThrowIfNull(configure);

        var arguments = string.Join(" ", args.Select(EscapeArgument));

        // Create a ProcessStartInfo to allow customization
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Allow user to customize
        configure(startInfo);

        // Create ProcessSpec from customized ProcessStartInfo
        var processSpec = new ProcessSpec(startInfo.FileName)
        {
            WorkingDirectory = startInfo.WorkingDirectory ?? workingDirectory,
            Arguments = startInfo.Arguments
        };

        // Transfer environment variables from ProcessStartInfo to ProcessSpec
        if (startInfo.Environment.Count > 0)
        {
            foreach (var kvp in startInfo.Environment)
            {
                if (kvp.Value is not null)
                {
                    processSpec.EnvironmentVariables[kvp.Key] = kvp.Value;
                }
            }
        }

        return new ExecStep(processSpec);
    }

    private static (string executable, string args) ParseCommandLine(string commandLine)
    {
        var trimmed = commandLine.Trim();
        
        // Handle quoted executable
        if (trimmed.StartsWith('"'))
        {
            var endQuoteIndex = trimmed.IndexOf('"', 1);
            if (endQuoteIndex > 0)
            {
                var executable = trimmed.Substring(1, endQuoteIndex - 1);
                var args = trimmed.Length > endQuoteIndex + 1 
                    ? trimmed.Substring(endQuoteIndex + 1).TrimStart() 
                    : string.Empty;
                return (executable, args);
            }
        }

        // Simple space-based split
        var firstSpaceIndex = trimmed.IndexOf(' ');
        if (firstSpaceIndex > 0)
        {
            return (trimmed.Substring(0, firstSpaceIndex), trimmed.Substring(firstSpaceIndex + 1).TrimStart());
        }

        // No arguments
        return (trimmed, string.Empty);
    }

    private static string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
        {
            return "\"\"";
        }

        // If the argument contains spaces or quotes, wrap it in quotes and escape internal quotes
        if (arg.Contains(' ') || arg.Contains('"'))
        {
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        }

        return arg;
    }
}
