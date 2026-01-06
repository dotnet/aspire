// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a source of input data for a process.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public abstract record ProcessInput
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessInput"/> class.
    /// </summary>
    protected ProcessInput() { }

    /// <summary>
    /// Gets an input source that provides no input. This is the default.
    /// </summary>
    public static ProcessInput Null { get; } = NullInput.s_instance;

    /// <summary>
    /// Gets an input source that exposes the input pipe for manual writing
    /// via <see cref="IProcessPipes.Input"/>.
    /// </summary>
    public static ProcessInput Pipe { get; } = PipeInput.s_instance;

    /// <summary>
    /// Creates an input source from a text string.
    /// </summary>
    /// <param name="text">The text to write to stdin.</param>
    /// <param name="encoding">The encoding to use, or null for UTF-8.</param>
    /// <returns>An input source.</returns>
    public static ProcessInput FromText(string text, Encoding? encoding = null)
        => new TextInput(text, encoding ?? Encoding.UTF8);

    /// <summary>
    /// Creates an input source from a byte array.
    /// </summary>
    /// <param name="bytes">The bytes to write to stdin.</param>
    /// <returns>An input source.</returns>
    public static ProcessInput FromBytes(ReadOnlyMemory<byte> bytes)
        => new BytesInput(bytes);

    /// <summary>
    /// Creates an input source from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after reading.</param>
    /// <returns>An input source.</returns>
    public static ProcessInput FromStream(Stream stream, bool leaveOpen = false)
        => new StreamInput(stream, leaveOpen);

    /// <summary>
    /// Creates an input source from a file.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <returns>An input source.</returns>
    public static ProcessInput FromFile(string path)
        => new FileInput(path);

    /// <summary>
    /// Creates an input source from an async writer function.
    /// </summary>
    /// <param name="writeAsync">A function that writes to the stdin stream.</param>
    /// <returns>An input source.</returns>
    public static ProcessInput FromWriter(Func<Stream, CancellationToken, Task> writeAsync)
        => new WriterInput(writeAsync);

    /// <summary>
    /// Writes the input data to the specified pipe writer.
    /// </summary>
    /// <param name="writer">The pipe writer to write to.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A task representing the write operation.</returns>
    public abstract Task WriteAsync(PipeWriter writer, CancellationToken ct);

    /// <summary>
    /// Gets whether the pipe should be completed after writing.
    /// When true (the default), the pipe is completed after <see cref="WriteAsync"/> returns.
    /// When false, the caller is responsible for completing the pipe.
    /// </summary>
    public virtual bool AutoComplete => true;

    internal sealed record NullInput : ProcessInput
    {
        internal static readonly NullInput s_instance = new();
        private NullInput() { }

        /// <inheritdoc />
        public override Task WriteAsync(PipeWriter writer, CancellationToken ct) => Task.CompletedTask;
    }

    internal sealed record PipeInput : ProcessInput
    {
        internal static readonly PipeInput s_instance = new();
        private PipeInput() { }

        /// <inheritdoc />
        public override bool AutoComplete => false;

        /// <inheritdoc />
        public override Task WriteAsync(PipeWriter writer, CancellationToken ct) => Task.CompletedTask;
    }

    internal sealed record TextInput(string Text, Encoding Encoding) : ProcessInput
    {
        /// <inheritdoc />
        public override async Task WriteAsync(PipeWriter writer, CancellationToken ct)
        {
            var bytes = Encoding.GetBytes(Text);
            await writer.WriteAsync(bytes, ct).ConfigureAwait(false);
        }
    }

    internal sealed record BytesInput(ReadOnlyMemory<byte> Bytes) : ProcessInput
    {
        /// <inheritdoc />
        public override async Task WriteAsync(PipeWriter writer, CancellationToken ct)
        {
            await writer.WriteAsync(Bytes, ct).ConfigureAwait(false);
        }
    }

    internal sealed record StreamInput(Stream Stream, bool LeaveOpen) : ProcessInput
    {
        /// <inheritdoc />
        public override async Task WriteAsync(PipeWriter writer, CancellationToken ct)
        {
            try
            {
                await Stream.CopyToAsync(writer, ct).ConfigureAwait(false);
            }
            finally
            {
                if (!LeaveOpen)
                {
                    await Stream.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    internal sealed record FileInput(string Path) : ProcessInput
    {
        /// <inheritdoc />
        public override async Task WriteAsync(PipeWriter writer, CancellationToken ct)
        {
            var fileStream = File.OpenRead(Path);
            await using (fileStream.ConfigureAwait(false))
            {
                await fileStream.CopyToAsync(writer, ct).ConfigureAwait(false);
            }
        }
    }

    internal sealed record WriterInput(Func<Stream, CancellationToken, Task> Writer) : ProcessInput
    {
        /// <inheritdoc />
        public override async Task WriteAsync(PipeWriter writer, CancellationToken ct)
        {
            var pipeStream = writer.AsStream(leaveOpen: true);
            await Writer(pipeStream, ct).ConfigureAwait(false);
            await pipeStream.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}
