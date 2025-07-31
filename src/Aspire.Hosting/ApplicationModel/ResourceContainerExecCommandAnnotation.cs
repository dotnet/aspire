// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a command annotation for a resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class ResourceContainerExecCommandAnnotation : ResourceCommandAnnotationBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceContainerExecCommandAnnotation"/> class.
    /// </summary>
    public ResourceContainerExecCommandAnnotation(
        string name,
        string displayName,
        string command,
        string? workingDirectory,
        string? displayDescription,
        object? parameter,
        string? confirmationMessage,
        string? iconName,
        IconVariant? iconVariant,
        bool isHighlighted)
        : base(name, displayName, displayDescription, parameter, confirmationMessage, iconName, iconVariant, isHighlighted)
    {
        Command = command;
        WorkingDirectory = workingDirectory;
    }

    /// <summary>
    /// The command to execute in the container.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// The working directory in the container where the command will be executed.
    /// </summary>
    public string? WorkingDirectory { get; }
}

/// <summary>
/// The output of a command executed in a container.
/// </summary>
public class ContainerExecCommandOutput
{
    /// <summary>
    /// The text output of the command.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Indicates whether the output is an error message.
    /// </summary>
    public required bool IsErrorMessage { get; init; }

    /// <summary>
    /// Represents the line number in the output where this message originated, if applicable.
    /// </summary>
    public int? LineNumber { get; init; }
}
