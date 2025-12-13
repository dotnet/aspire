// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

namespace Aspire.Hosting.Execution;

/// <summary>
/// Specifies options for executing a CLI command.
/// </summary>
public sealed class ExecSpec
{
    /// <summary>
    /// Gets or sets the working directory for the process.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets the environment variables to set for the process.
    /// A null value removes the variable from the environment.
    /// </summary>
    public Dictionary<string, string?> Environment { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets a value indicating whether to capture stdout and stderr.
    /// Defaults to true for Run/Cap methods, false for streaming methods.
    /// </summary>
    public bool CaptureOutput { get; set; } = true;

    /// <summary>
    /// Gets or sets the stdin source for the process.
    /// </summary>
    public Stdin? Stdin { get; set; }

    /// <summary>
    /// Gets or sets the stdout target for the process.
    /// </summary>
    public StdoutTarget? Stdout { get; set; }

    /// <summary>
    /// Gets or sets the stderr target for the process.
    /// </summary>
    public StderrTarget? Stderr { get; set; }
}
