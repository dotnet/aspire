// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Thrown when an error occurs when executing the DevTunnel tool.
/// </summary>
public class DevTunnelToolException(string message, string? command = null, string? stdout = null, string? stderr = null) : Exception(message)
{
    /// <summary>
    /// The command that was executed when the error occurred.
    /// </summary>
    public string? Command { get; } = command;

    /// <summary>
    /// Contains stdout output from command.
    /// </summary>
    public string? Stdout { get; } = stdout;

    /// <summary>
    /// Contains stderr output from command.
    /// </summary>
    public string? Stderr { get; } = stderr;
}
