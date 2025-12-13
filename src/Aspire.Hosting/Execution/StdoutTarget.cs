// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a target for stdout data from a process.
/// </summary>
public abstract record StdoutTarget
{
    private StdoutTarget() { }

    /// <summary>
    /// Captures stdout into the result's Stdout property.
    /// </summary>
    /// <returns>A stdout target that captures output.</returns>
    public static StdoutTarget Capture() => CaptureStdoutTarget.Instance;

    /// <summary>
    /// Streams stdout lines to a callback.
    /// </summary>
    /// <param name="write">The callback to invoke for each line.</param>
    /// <returns>A stdout target that streams to the callback.</returns>
    public static StdoutTarget LineWriter(Action<string> write)
        => new LineWriterStdoutTarget(write);

    /// <summary>
    /// Writes stdout to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after writing.</param>
    /// <returns>A stdout target that writes to the stream.</returns>
    public static StdoutTarget ToStream(Stream stream, bool leaveOpen = false)
        => new StreamStdoutTarget(stream, leaveOpen);

    /// <summary>
    /// Tees stdout to multiple targets.
    /// </summary>
    /// <param name="a">The first target.</param>
    /// <param name="b">The second target.</param>
    /// <returns>A stdout target that writes to both targets.</returns>
    public static StdoutTarget Tee(StdoutTarget a, StdoutTarget b)
        => new TeeStdoutTarget(a, b);

    internal sealed record CaptureStdoutTarget : StdoutTarget
    {
        public static CaptureStdoutTarget Instance { get; } = new();
    }

    internal sealed record LineWriterStdoutTarget(Action<string> Write) : StdoutTarget;
    internal sealed record StreamStdoutTarget(Stream Stream, bool LeaveOpen) : StdoutTarget;
    internal sealed record TeeStdoutTarget(StdoutTarget A, StdoutTarget B) : StdoutTarget;
}
