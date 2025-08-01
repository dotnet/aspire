// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a command annotation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class ResourceExecCommandAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceExecCommandAnnotation"/> class.
    /// </summary>
    public ResourceExecCommandAnnotation(
        string name,
        string displayName,
        string command,
        string? workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(command);

        Name = name;
        DisplayName = displayName;
        Command = command;
        WorkingDirectory = workingDirectory;
    }

    /// <summary>
    /// The name of the command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The display name of the command.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The command to execute.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// The working directory in which the command will be executed.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
