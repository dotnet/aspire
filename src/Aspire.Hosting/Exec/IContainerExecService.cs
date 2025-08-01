// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Exec;

/// <summary>
/// A service to execute container exec commands.
/// </summary>
public interface IContainerExecService
{
    /// <summary>
    /// Runs the command in the container resource.
    /// </summary>
    /// <param name="containerResource">Container resource to run a command in.</param>
    /// <param name="commandName">The command name to run. Should match the command name from <see cref="ResourceBuilderExtensions.WithExecCommand{T}(IResourceBuilder{T}, string, string, string, string?, CommandOptions?)"/></param>
    /// <returns>Returns the type representing command execution run. Allows to await on the command completion and reading execution logs.</returns>
    ExecCommandRun ExecuteCommand(ContainerResource containerResource, string commandName);

    /// <summary>
    /// Runs the command in the container resource.
    /// </summary>
    /// <param name="resourceId">Id of the container resource to execute command in.</param>
    /// <param name="commandName">The command name to run. Should match the command name from <see cref="ResourceBuilderExtensions.WithExecCommand{T}(IResourceBuilder{T}, string, string, string, string?, CommandOptions?)"/></param>
    /// <returns>Returns the type representing command execution run. Allows to await on the command completion and reading execution logs.</returns>
    ExecCommandRun ExecuteCommand(string resourceId, string commandName);
}

/// <summary>
/// Represents the result of starting a ContainerExec 
/// </summary>
public class ExecCommandRun
{
    /// <summary>
    /// Function that can be awaited to run the command and get its result.
    /// </summary>
    public required Func<CancellationToken, Task<ExecuteCommandResult>> ExecuteCommand { get; init; }

    /// <summary>
    /// Function that can be used to get the output stream of the command execution.
    /// </summary>
    public Func<CancellationToken, IAsyncEnumerable<LogLine>> GetOutputStream { get; init; } = EmptyOutput;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<LogLine> EmptyOutput([EnumeratorCancellation] CancellationToken _)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        yield break;
    }
}
