// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;

namespace Aspire.Hosting.Utils;

/// <summary>
/// A stream capable of computing the hash digest of raw data while also copying it.
/// </summary>
internal sealed class HashDigestStream : Stream
{
    private readonly Stream _writeStream;
    private readonly NonCryptographicHashAlgorithm _hashAlgorithm;

    public HashDigestStream(Stream writeStream, NonCryptographicHashAlgorithm hashAlgorithm)
    {
        _writeStream = writeStream;
        _hashAlgorithm = hashAlgorithm;
    }

    public override bool CanWrite => true;

    public override void Write(byte[] buffer, int offset, int count)
    {
        _hashAlgorithm.Append(buffer.AsSpan(offset, count));
        _writeStream.Write(buffer, offset, count);
    }

    public override void Flush()
    {
        _writeStream.Flush();
    }

    // This should not be used by Stream.CopyTo(Stream)
    public override void Write(ReadOnlySpan<byte> buffer)
        => throw new NotImplementedException();

    // This class is never used with async writes, but if it ever is, implement these overrides
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => throw new NotImplementedException();
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override long Length => throw new NotImplementedException();
    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
    public override void SetLength(long value) => throw new NotImplementedException();
}
