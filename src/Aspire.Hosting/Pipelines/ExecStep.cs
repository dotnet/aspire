#pragma warning disable ASPIREPIPELINES001

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Provides factory methods to create pipeline steps that run executable processes.
/// </summary>
[Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class ExecStep
{
    /// <summary>
    /// Creates a pipeline step from a full command line string.
    /// </summary>
    /// <param name="name">The unique name for the pipeline step.</param>
    /// <param name="commandLine">The full command line including the executable and arguments.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <returns>A <see cref="PipelineStep"/> that executes the specified command.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/>, <paramref name="commandLine"/>, or <paramref name="workingDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="commandLine"/> is empty or whitespace.</exception>
    /// <remarks>
    /// The command line will be parsed to extract the executable path and arguments.
    /// </remarks>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static PipelineStep Create(string name, string commandLine, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var (executable, args) = CommandLineArgsParser.ParseCommand(commandLine);

        return new PipelineStep
        {
            Name = name,
            Action = async context =>
            {
                var processSpec = new ProcessSpec(executable)
                {
                    WorkingDirectory = workingDirectory,
                    Arguments = string.Join(" ", args),
                    OnOutputData = output =>
                    {
                        context.Logger.LogDebug("{Executable} (stdout): {Output}", executable, output);
                    },
                    OnErrorData = error =>
                    {
                        context.Logger.LogDebug("{Executable} (stderr): {Error}", executable, error);
                    },
                    ThrowOnNonZeroReturnCode = false
                };

                context.Logger.LogDebug("Running {Executable} with arguments: {Arguments}", executable, processSpec.Arguments);
                var (resultTask, disposable) = ProcessUtil.Run(processSpec);
                await using (disposable.ConfigureAwait(false))
                {
                    var result = await resultTask.ConfigureAwait(false);
                    
                    if (result.ExitCode != 0)
                    {
                        context.Logger.LogError("{Executable} failed with exit code {ExitCode}", executable, result.ExitCode);
                        throw new DistributedApplicationException($"{executable} failed with exit code {result.ExitCode}.");
                    }

                    context.Logger.LogInformation("{Executable} succeeded", executable);
                }
            }
        };
    }

    /// <summary>
    /// Creates a pipeline step from an executable path and separate arguments.
    /// </summary>
    /// <param name="name">The unique name for the pipeline step.</param>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <returns>A <see cref="PipelineStep"/> that executes the specified command.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/>, <paramref name="executable"/>, <paramref name="args"/>, or <paramref name="workingDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="executable"/> is empty or whitespace.</exception>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static PipelineStep Create(string name, string executable, string[] args, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var arguments = string.Join(" ", args.Select(EscapeArgument));

        return new PipelineStep
        {
            Name = name,
            Action = async context =>
            {
                var processSpec = new ProcessSpec(executable)
                {
                    WorkingDirectory = workingDirectory,
                    Arguments = arguments,
                    OnOutputData = output =>
                    {
                        context.Logger.LogDebug("{Executable} (stdout): {Output}", executable, output);
                    },
                    OnErrorData = error =>
                    {
                        context.Logger.LogDebug("{Executable} (stderr): {Error}", executable, error);
                    },
                    ThrowOnNonZeroReturnCode = false
                };

                context.Logger.LogDebug("Running {Executable} with arguments: {Arguments}", executable, processSpec.Arguments);
                var (resultTask, disposable) = ProcessUtil.Run(processSpec);
                await using (disposable.ConfigureAwait(false))
                {
                    var result = await resultTask.ConfigureAwait(false);
                    
                    if (result.ExitCode != 0)
                    {
                        context.Logger.LogError("{Executable} failed with exit code {ExitCode}", executable, result.ExitCode);
                        throw new DistributedApplicationException($"{executable} failed with exit code {result.ExitCode}.");
                    }

                    context.Logger.LogInformation("{Executable} succeeded", executable);
                }
            }
        };
    }

    /// <summary>
    /// Creates a pipeline step with the ability to customize the process specification.
    /// </summary>
    /// <param name="name">The unique name for the pipeline step.</param>
    /// <param name="executable">The path to the executable to run.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="configure">A callback to customize the <see cref="ProcessStartInfo"/> before the process starts.</param>
    /// <returns>A <see cref="PipelineStep"/> that executes the specified command.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/>, <paramref name="executable"/>, <paramref name="args"/>, <paramref name="workingDirectory"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="executable"/> is empty or whitespace.</exception>
    [Experimental("ASPIREEXECSTEP001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public static PipelineStep Create(string name, string executable, string[] args, string workingDirectory, Action<ProcessStartInfo> configure)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(executable);
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        ArgumentNullException.ThrowIfNull(configure);

        var arguments = string.Join(" ", args.Select(EscapeArgument));

        return new PipelineStep
        {
            Name = name,
            Action = async context =>
            {
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
                    Arguments = startInfo.Arguments,
                    OnOutputData = output =>
                    {
                        context.Logger.LogDebug("{Executable} (stdout): {Output}", startInfo.FileName, output);
                    },
                    OnErrorData = error =>
                    {
                        context.Logger.LogDebug("{Executable} (stderr): {Error}", startInfo.FileName, error);
                    },
                    ThrowOnNonZeroReturnCode = false
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

                context.Logger.LogDebug("Running {Executable} with arguments: {Arguments}", startInfo.FileName, processSpec.Arguments);
                var (resultTask, disposable) = ProcessUtil.Run(processSpec);
                await using (disposable.ConfigureAwait(false))
                {
                    var result = await resultTask.ConfigureAwait(false);
                    
                    if (result.ExitCode != 0)
                    {
                        context.Logger.LogError("{Executable} failed with exit code {ExitCode}", startInfo.FileName, result.ExitCode);
                        throw new DistributedApplicationException($"{startInfo.FileName} failed with exit code {result.ExitCode}.");
                    }

                    context.Logger.LogInformation("{Executable} succeeded", startInfo.FileName);
                }
            }
        };
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
