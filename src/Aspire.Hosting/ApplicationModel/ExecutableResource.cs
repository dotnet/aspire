// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that can be executed as a standalone process.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="command">The command to execute.</param>
/// <param name="workingDirectory">The working directory of the executable.</param>
/// <param name="args">The arguments to pass to the executable.</param>
public class ExecutableResource(string name, string command, string workingDirectory, string[]? args) : Resource(name), IResourceWithEnvironment, IResourceWithBindings
{
    /// <summary>
    /// Gets the command associated with this executable resource.
    /// </summary>
    public string Command { get; } = command;

    /// <summary>
    /// Gets the working directory for the executable resource.
    /// </summary>
    public string WorkingDirectory { get; } = workingDirectory;
    
    /// <summary>
    /// Gets the command line arguments passed to the executable resource.
    /// </summary>
    public string[]? Args { get; } = args;
}
