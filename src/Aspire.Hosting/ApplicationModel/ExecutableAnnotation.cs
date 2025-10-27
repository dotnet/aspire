// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for a container image.
/// </summary>
[DebuggerDisplay("Command = {Command,nq}, WorkingDirectory = {WorkingDirectory}")]
public sealed class ExecutableAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the command associated with this executable resource.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Gets or sets the working directory for the executable resource.
    /// </summary>
    public required string WorkingDirectory { get; set; }
}
