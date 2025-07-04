// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Exec;

/// <summary>
/// Configuration options for running AppHost in exec mode.
/// </summary>
internal sealed class ExecOptions
{
    /// <summary>
    /// The name of the exec configuration section in the appsettings.json file.
    /// </summary>
    public const string SectionName = "Exec";

    /// <summary>
    /// Represents whether the apphost is running in exec mode.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Target resource to execute the command against.
    /// </summary>
    public required string ResourceName { get; set; }

    /// <summary>
    /// Command to execute against the target resource by <see cref="ResourceName"/>.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Whether to start the <see cref="ResourceName"/> resource before executing the command.
    /// By default is false.
    /// </summary>
    public bool StartResource { get; set; }
}
