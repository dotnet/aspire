// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// Represents a single log entry for a resource.
/// </summary>
internal class ResourceLogEntry
{
    /// <summary>
    /// Gets the log line content.
    /// </summary>
    public required string Line { get; init; }

    /// <summary>
    /// Gets the stream designation for the log entry.
    /// </summary>
    public required LogEntryStream Stream { get; init; }
}

/// <summary>
/// Represents the stream type for a log entry.
/// </summary>
internal enum LogEntryStream
{
    /// <summary>
    /// Standard output stream.
    /// </summary>
    StdOut,

    /// <summary>
    /// Standard error stream.
    /// </summary>
    StdErr
}