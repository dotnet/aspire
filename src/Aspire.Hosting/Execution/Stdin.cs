// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a source of stdin data for a process.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public abstract record Stdin
{
    private Stdin() { }

    /// <summary>
    /// Gets whether this stdin source is for manual pipe writing (no auto-complete).
    /// </summary>
    internal virtual bool IsPipe => false;

    /// <summary>
    /// Creates a stdin source from a text string.
    /// </summary>
    /// <param name="text">The text to write to stdin.</param>
    /// <param name="encoding">The encoding to use, or null for UTF-8.</param>
    /// <returns>A stdin source.</returns>
    public static Stdin FromText(string text, Encoding? encoding = null)
        => new TextStdin(text, encoding ?? Encoding.UTF8);

    /// <summary>
    /// Creates a stdin source from a byte array.
    /// </summary>
    /// <param name="bytes">The bytes to write to stdin.</param>
    /// <returns>A stdin source.</returns>
    public static Stdin FromBytes(ReadOnlyMemory<byte> bytes)
        => new BytesStdin(bytes);

    /// <summary>
    /// Creates a stdin source from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after reading.</param>
    /// <returns>A stdin source.</returns>
    public static Stdin FromStream(Stream stream, bool leaveOpen = false)
        => new StreamStdin(stream, leaveOpen);

    /// <summary>
    /// Creates a stdin source from a file.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <returns>A stdin source.</returns>
    public static Stdin FromFile(string path)
        => new FileStdin(path);

    /// <summary>
    /// Creates a stdin source from an async writer function.
    /// </summary>
    /// <param name="writeAsync">A function that writes to the stdin stream.</param>
    /// <returns>A stdin source.</returns>
    public static Stdin FromWriter(Func<Stream, CancellationToken, Task> writeAsync)
        => new WriterStdin(writeAsync);

    /// <summary>
    /// Creates a stdin source that exposes the input pipe for manual writing.
    /// </summary>
    internal static Stdin CreatePipe() => PipeStdin.s_instance;

    internal sealed record TextStdin(string Text, Encoding Encoding) : Stdin;
    internal sealed record BytesStdin(ReadOnlyMemory<byte> Bytes) : Stdin;
    internal sealed record StreamStdin(Stream Stream, bool LeaveOpen) : Stdin;
    internal sealed record FileStdin(string Path) : Stdin;
    internal sealed record WriterStdin(Func<Stream, CancellationToken, Task> WriteAsync) : Stdin;
    internal sealed record PipeStdin : Stdin
    {
        internal static readonly PipeStdin s_instance = new();
        private PipeStdin() { }
        internal override bool IsPipe => true;
    }
}
