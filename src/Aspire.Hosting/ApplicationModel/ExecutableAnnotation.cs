// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for an executable resource.
/// </summary>
/// <param name="command">The command to execute.</param>
/// <param name="workingDirectory">The working directory of the executable.</param>
public class ExecutableAnnotation(string command, string workingDirectory) : IResourceAnnotation
{
    /// <summary>
    /// Gets the command associated with this executable resource.
    /// </summary>
    public string Command { get; } = command;

    /// <summary>
    /// Gets the working directory for the executable resource.
    /// </summary>
    public string WorkingDirectory { get; } = workingDirectory;
}
