// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a target for stderr data from a process.
/// </summary>
public abstract record StderrTarget
{
    private StderrTarget() { }

    /// <summary>
    /// Captures stderr into the result's Stderr property.
    /// </summary>
    /// <returns>A stderr target that captures output.</returns>
    public static StderrTarget Capture() => CaptureStderrTarget.Instance;

    /// <summary>
    /// Streams stderr lines to a callback.
    /// </summary>
    /// <param name="write">The callback to invoke for each line.</param>
    /// <returns>A stderr target that streams to the callback.</returns>
    public static StderrTarget LineWriter(Action<string> write)
        => new LineWriterStderrTarget(write);

    /// <summary>
    /// Writes stderr to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after writing.</param>
    /// <returns>A stderr target that writes to the stream.</returns>
    public static StderrTarget ToStream(Stream stream, bool leaveOpen = false)
        => new StreamStderrTarget(stream, leaveOpen);

    /// <summary>
    /// Tees stderr to multiple targets.
    /// </summary>
    /// <param name="a">The first target.</param>
    /// <param name="b">The second target.</param>
    /// <returns>A stderr target that writes to both targets.</returns>
    public static StderrTarget Tee(StderrTarget a, StderrTarget b)
        => new TeeStderrTarget(a, b);

    internal sealed record CaptureStderrTarget : StderrTarget
    {
        public static CaptureStderrTarget Instance { get; } = new();
    }

    internal sealed record LineWriterStderrTarget(Action<string> Write) : StderrTarget;
    internal sealed record StreamStderrTarget(Stream Stream, bool LeaveOpen) : StderrTarget;
    internal sealed record TeeStderrTarget(StderrTarget A, StderrTarget B) : StderrTarget;
}
