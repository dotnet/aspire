// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Executable resource that runs in a container.
/// </summary>
internal class ContainerExecutableResource(string name, ContainerResource containerResource, string command, string? workingDirectory)
    : Resource(name), IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport
{
    /// <summary>
    /// Gets the command associated with this executable resource.
    /// </summary>
    public string Command { get; } = ThrowIfNullOrEmpty(command);

    /// <summary>
    /// Gets the working directory for the executable resource.
    /// </summary>
    public string? WorkingDirectory { get; } = workingDirectory;

    /// <summary>
    /// Args of the command to run in the container.
    /// </summary>
    public ICollection<string>? Args { get; init; }

    /// <summary>
    /// Target container resource that this executable runs in.
    /// </summary>
    public ContainerResource? TargetContainerResource { get; } = containerResource ?? throw new ArgumentNullException(nameof(containerResource));

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
