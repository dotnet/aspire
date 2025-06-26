// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Represents a single line of output to be displayed, along with its associated stream (such as "stdout" or "stderr").
/// </summary>
internal sealed class DisplayLineState(string stream, string line)
{
    /// <summary>
    /// Gets the name of the stream the line belongs to (e.g., "stdout", "stderr").
    /// </summary>
    public string Stream { get; } = stream;

    /// <summary>
    /// Gets the content of the line to be displayed.
    /// </summary>
    public string Line { get; } = line;
}

internal class BackchannelLogEntry
{
    public required EventId EventId { get; set; }
    public required LogLevel LogLevel { get; set; }
    public required string Message { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
    public required string CategoryName { get; set; }
}
