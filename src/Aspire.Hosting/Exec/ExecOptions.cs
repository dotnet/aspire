// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Exec;

/// <summary>
/// Options for launching AppHost in exec mode
/// </summary>
public class ExecOptions
{
    /// <summary>
    /// The name of the exec section in the configuration
    /// </summary>
    public const string SectionName = "exec";

    /// <summary>
    /// Target resource to exec the command against.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Command to exec against the target resource.
    /// </summary>
    public required string Command { get; set; }
}
