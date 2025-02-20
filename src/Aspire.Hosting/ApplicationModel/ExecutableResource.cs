// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified executable process.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="command">The command to execute.</param>
/// <param name="workingDirectory">The working directory of the executable.</param>
/// <param name="commandParamName"></param>
/// <param name="workingDirectoryParamName"></param>
public class ExecutableResource(string name, string command, string workingDirectory,
    [CallerArgumentExpression(nameof(command))] string? commandParamName = null,
    [CallerArgumentExpression(nameof(workingDirectory))] string? workingDirectoryParamName = null)
    : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport
{
    /// <summary>
    /// Gets the command associated with this executable resource.
    /// </summary>
    public string Command { get; } = ThrowIfNullOrEmpty(command, commandParamName);

    /// <summary>
    /// Gets the working directory for the executable resource.
    /// </summary>
    public string WorkingDirectory { get; } = ThrowIfNullOrEmpty(workingDirectory, workingDirectoryParamName);

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
