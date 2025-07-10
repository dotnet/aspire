// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

internal class ContainerExecutableResource(string name, string containerName, string command, string? workingDirectory)
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

    public string ContainerName { get; } = ThrowIfNullOrEmpty(containerName);

    public ICollection<string>? Args { get; init; }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
