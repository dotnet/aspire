// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh;

internal sealed class ScriptInjectingStream : Stream
{
    internal static string InjectedScript { get; } = $"<script src=\"{ApplicationPaths.BrowserRefreshJS}\"></script>";

    private static readonly ReadOnlyMemory<byte> s_bodyTagBytes = "</body>"u8.ToArray();
    private static readonly ReadOnlyMemory<byte> s_injectedScriptBytes = Encoding.UTF8.GetBytes(InjectedScript);

    private readonly Stream _baseStream;

    private int _partialBodyTagLength;
    private bool _isDisposed;

    public ScriptInjectingStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length { get; }
    public override long Position { get; set; }
    public bool ScriptInjectionPerformed { get; private set; }

    public override void Flush()
        => _baseStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken)
        => _baseStream.FlushAsync(cancellationToken);

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!ScriptInjectionPerformed)
        {
            ScriptInjectionPerformed = TryInjectScript(buffer);
        }
        else
        {
            _baseStream.Write(buffer);
        }
    }

    public override void WriteByte(byte value)
    {
        _baseStream.WriteByte(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!ScriptInjectionPerformed)
        {
            ScriptInjectionPerformed = TryInjectScript(buffer.AsSpan(offset, count));
        }
        else
        {
            _baseStream.Write(buffer, offset, count);
        }
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (!ScriptInjectionPerformed)
        {
            ScriptInjectionPerformed = await TryInjectScriptAsync(buffer.AsMemory(offset, count), cancellationToken);
        }
        else
        {
            await _baseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
        }
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!ScriptInjectionPerformed)
        {
            ScriptInjectionPerformed = await TryInjectScriptAsync(buffer, cancellationToken);
        }
        else
        {
            await _baseStream.WriteAsync(buffer, cancellationToken);
        }
    }

    private bool TryInjectScript(ReadOnlySpan<byte> buffer)
    {
        var sourceBuffer = new SourceBuffer(buffer);
        var writer = new SyncBaseStreamWriter(_baseStream);
        return TryInjectScriptCore(ref writer, sourceBuffer);
    }

    private async ValueTask<bool> TryInjectScriptAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        var sourceBuffer = new SourceBuffer(buffer.Span);
        var writer = new AsyncBaseStreamWriter(_baseStream, buffer);
        var result = TryInjectScriptCore(ref writer, sourceBuffer);

        await writer.WriteToBaseStreamAsync(cancellationToken);

        return result;
    }

    // Implements the core script injection logic in a manner agnostic to whether writes
    // are synchronous or asynchronous.
    private bool TryInjectScriptCore<TWriter>(ref TWriter writer, SourceBuffer buffer)
        where TWriter : struct, IBaseStreamWriter
    {
        if (_partialBodyTagLength != 0)
        {
            // We're in the middle of parsing a potential body tag,
            // which means that the rest of the body tag must be
            // at the start of the buffer.

            var restPartialTagLength = FindPartialTagLengthFromStart(currentBodyTagLength: _partialBodyTagLength, buffer.Span);
            if (restPartialTagLength == -1)
            {
                // This wasn't a closing body tag. Flush what we've buffered so far and reset.
                // We don't return here because we want to continue to process the buffer as if
                // we weren't reading a partial body tag.
                writer.Write(s_bodyTagBytes[.._partialBodyTagLength]);
                _partialBodyTagLength = 0;
            }
            else
            {
                // This may still be a closing body tag.
                _partialBodyTagLength += restPartialTagLength;

                Debug.Assert(_partialBodyTagLength <= s_bodyTagBytes.Length);

                if (_partialBodyTagLength == s_bodyTagBytes.Length)
                {
                    // We've just read a full closing body tag, so we flush it to the stream.
                    // Then just write the rest of the stream normally as we've now finished searching
                    // for the script.
                    writer.Write(s_injectedScriptBytes);
                    writer.Write(s_bodyTagBytes);
                    writer.Write(buffer[restPartialTagLength..]);
                    _partialBodyTagLength = 0;
                    return true;
                }
                else
                {
                    // We're still in the middle of reading the body tag,
                    // so there's nothing else to flush to the stream.
                    return false;
                }
            }
        }

        // We now know we're not in the middle of processing a body tag.
        Debug.Assert(_partialBodyTagLength == 0);

        var index = buffer.Span.LastIndexOf(s_bodyTagBytes.Span);
        if (index == -1)
        {
            // We didn't find the full closing body tag in the buffer, but the end of the buffer
            // might contain the start of a closing body tag.

            var partialBodyTagLength = FindPartialTagLengthFromEnd(buffer.Span);
            if (partialBodyTagLength == -1)
            {
                // We know that the end of the buffer definitely does not
                // represent a closing body tag. We'll just flush the buffer
                // to the base stream.
                writer.Write(buffer);
                return false;
            }
            else
            {
                // We might have found a body tag at the end of the buffer.
                // We'll write the buffer leading up to the start of the body
                // tag candidate.

                writer.Write(buffer[..^partialBodyTagLength]);
                _partialBodyTagLength = partialBodyTagLength;
                return false;
            }
        }

        if (index > 0)
        {
            writer.Write(buffer[..index]);
            buffer = buffer[index..];
        }

        // Write the injected script
        writer.Write(s_injectedScriptBytes);

        // Write the rest of the buffer/HTML doc
        writer.Write(buffer);
        return true;
    }

    private static int FindPartialTagLengthFromStart(int currentBodyTagLength, ReadOnlySpan<byte> buffer)
    {
        var remainingBodyTagBytes = s_bodyTagBytes.Span[currentBodyTagLength..];
        var minLength = Math.Min(buffer.Length, remainingBodyTagBytes.Length);

        return buffer[..minLength].SequenceEqual(remainingBodyTagBytes[..minLength])
            ? minLength
            : -1;
    }

    private static int FindPartialTagLengthFromEnd(ReadOnlySpan<byte> buffer)
    {
        var bufferLength = buffer.Length;
        if (bufferLength == 0)
        {
            return -1;
        }

        // Since each character within "</body>" is unique, we can use the last byte
        // in the buffer to determine the length of the partial body tag.
        var lastByte = buffer[^1];
        var bodyMarkerIndexOfLastByte = BodyTagIndexOf(lastByte);
        if (bodyMarkerIndexOfLastByte == -1)
        {
            // The last character does not appear "</body>", so we know
            // there's not a partial body tag.
            return -1;
        }

        var partialTagLength = bodyMarkerIndexOfLastByte + 1;
        if (buffer.Length < partialTagLength)
        {
            // The buffer is shorter than the expected length of the partial
            // body tag, so we know the buffer can't possibly contain it.
            return -1;
        }

        // Finally, we need to check that the content at the end of the buffer
        // matches the expected partial body tag.
        return buffer[^partialTagLength..].SequenceEqual(s_bodyTagBytes.Span[..partialTagLength])
            ? partialTagLength
            : -1;

        // We can utilize the fact that each character is unique in "</body>"
        // to perform an efficient index lookup.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int BodyTagIndexOf(byte c)
            => c switch
            {
                (byte)'<' => 0,
                (byte)'/' => 1,
                (byte)'b' => 2,
                (byte)'o' => 3,
                (byte)'d' => 4,
                (byte)'y' => 5,
                (byte)'>' => 6,
                _ => -1,
            };
    }

    public ValueTask CompleteAsync() => DisposeAsync();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_partialBodyTagLength > 0)
        {
            // We might have buffered some data thinking that it could represent
            // a body tag. We know at this point that there's no more data
            // on its way, so we'll write the remaining data to the buffer.
            await _baseStream.WriteAsync(s_bodyTagBytes[.._partialBodyTagLength]);
            _partialBodyTagLength = 0;
        }

        await FlushAsync();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
         => throw new NotSupportedException();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
         => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    // A thin wrapper over ReadOnlySpan<byte> that keeps track of the current range relative
    // to the originally-provided buffer.
    // This enables the sharing of logic between scenarios where only a ReadOnlySpan<byte>
    // is available (some synchronous writes) and scenarios where ReadOnlyMemory<byte>
    // is required (all asynchronous writes).
    private readonly ref struct SourceBuffer
    {
        private readonly int _offsetFromOriginal;

        public readonly ReadOnlySpan<byte> Span;

        public int Length => Span.Length;

        public Range RangeInOriginal => new(_offsetFromOriginal, _offsetFromOriginal + Span.Length);

        public SourceBuffer(ReadOnlySpan<byte> span)
            : this(span, offsetFromOriginal: 0)
        {
        }

        private SourceBuffer(ReadOnlySpan<byte> span, int offsetFromOriginal)
        {
            Span = span;
            _offsetFromOriginal = offsetFromOriginal;
        }

        public SourceBuffer Slice(int start, int length)
            => new(Span.Slice(start, length), offsetFromOriginal: _offsetFromOriginal + start);
    }

    // Represents a writer to the base stream.
    // Accepts arbitrary heap-allocated buffers and
    // ranges of the source buffer.
    private interface IBaseStreamWriter
    {
        void Write(in SourceBuffer buffer);
        void Write(ReadOnlyMemory<byte> data);
    }

    // A base stream writer that performs synchronous writes.
    private struct SyncBaseStreamWriter(Stream baseStream) : IBaseStreamWriter
    {
        public readonly void Write(ReadOnlyMemory<byte> data) => baseStream.Write(data.Span);
        public readonly void Write(in SourceBuffer buffer) => baseStream.Write(buffer.Span);
    }

    // A base stream writer that enables buffering writes synchronously and applying
    // them to the base stream when an async context is available.
    private struct AsyncBaseStreamWriter(Stream baseStream, ReadOnlyMemory<byte> bufferMemory) : IBaseStreamWriter
    {
        private WriteBuffer _writes;
        private int _writeCount;

        public void Write(ReadOnlyMemory<byte> data) => _writes[_writeCount++] = data;
        public void Write(in SourceBuffer buffer) => _writes[_writeCount++] = bufferMemory[buffer.RangeInOriginal];

        public readonly async ValueTask WriteToBaseStreamAsync(CancellationToken cancellationToken)
        {
            for (var i = 0; i < _writeCount; i++)
            {
                await baseStream.WriteAsync(_writes[i], cancellationToken);
            }
        }

        // We don't currently need than 4 writes, but we can bump this in the future if needed.
        // If we ever target .NET 8+, we can use the [InlineArray] feature instead.
        private struct WriteBuffer
        {
            ReadOnlyMemory<byte> _write0;
            ReadOnlyMemory<byte> _write1;
            ReadOnlyMemory<byte> _write2;
            ReadOnlyMemory<byte> _write3;

            private static ref ReadOnlyMemory<byte> GetAt(ref WriteBuffer buffer, int index)
            {
                switch (index)
                {
                    case 0: return ref buffer._write0;
                    case 1: return ref buffer._write1;
                    case 2: return ref buffer._write2;
                    case 3: return ref buffer._write3;
                    default: throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public ReadOnlyMemory<byte> this[int index]
            {
                get => GetAt(ref this, index);
                set => GetAt(ref this, index) = value;
            }
        }
    }
}
