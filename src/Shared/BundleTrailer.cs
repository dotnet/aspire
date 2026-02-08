// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file is source-linked into multiple projects:
// - Aspire.Cli
// Do not add project-specific dependencies.

using System.Buffers.Binary;
using System.Text;

namespace Aspire.Shared;

/// <summary>
/// Reads and writes the trailer appended to a self-extracting Aspire CLI binary.
/// The trailer is the last 32 bytes of the file and describes the embedded tar.gz payload.
/// </summary>
internal static class BundleTrailer
{
    /// <summary>
    /// Total size of the trailer in bytes.
    /// </summary>
    public const int TrailerSize = 32;

    /// <summary>
    /// Magic bytes identifying a valid Aspire bundle trailer: "ASPIRE\0\0".
    /// </summary>
    private static readonly byte[] s_magic = "ASPIRE\0\0"u8.ToArray();

    /// <summary>
    /// Attempts to read a bundle trailer from the end of the specified file.
    /// </summary>
    /// <returns>The trailer info if valid, or null if the file has no embedded payload.</returns>
    public static BundleTrailerInfo? TryRead(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return TryRead(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to read a bundle trailer from the end of the specified stream.
    /// </summary>
    public static BundleTrailerInfo? TryRead(Stream stream)
    {
        if (stream.Length < TrailerSize)
        {
            return null;
        }

        stream.Seek(-TrailerSize, SeekOrigin.End);

        Span<byte> buffer = stackalloc byte[TrailerSize];
        if (stream.ReadAtLeast(buffer, TrailerSize, throwOnEndOfStream: false) < TrailerSize)
        {
            return null;
        }

        // Validate magic bytes
        if (!buffer[..8].SequenceEqual(s_magic))
        {
            return null;
        }

        var payloadOffset = BinaryPrimitives.ReadUInt64LittleEndian(buffer[8..16]);
        var payloadSize = BinaryPrimitives.ReadUInt64LittleEndian(buffer[16..24]);
        var versionHash = BinaryPrimitives.ReadUInt64LittleEndian(buffer[24..32]);

        // Basic validation
        if (payloadOffset + payloadSize + TrailerSize != (ulong)stream.Length)
        {
            return null;
        }

        return new BundleTrailerInfo(payloadOffset, payloadSize, versionHash);
    }

    /// <summary>
    /// Writes a trailer to the end of the specified stream. The stream should already
    /// contain the native CLI binary followed by the tar.gz payload.
    /// </summary>
    public static void Write(Stream stream, ulong payloadOffset, ulong payloadSize, ulong versionHash)
    {
        Span<byte> buffer = stackalloc byte[TrailerSize];

        s_magic.CopyTo(buffer);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[8..16], payloadOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[16..24], payloadSize);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer[24..32], versionHash);

        stream.Write(buffer);
    }

    /// <summary>
    /// Computes a simple version hash from a version string.
    /// </summary>
    public static ulong ComputeVersionHash(string version)
    {
        var bytes = Encoding.UTF8.GetBytes(version);
        ulong hash = 14695981039346656037; // FNV-1a offset basis
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= 1099511628211; // FNV-1a prime
        }
        return hash;
    }

    /// <summary>
    /// Opens a read-only stream over the embedded payload in the specified file.
    /// </summary>
    public static Stream OpenPayload(string filePath, BundleTrailerInfo trailer)
    {
        var stream = File.OpenRead(filePath);
        stream.Seek((long)trailer.PayloadOffset, SeekOrigin.Begin);
        return new SubStream(stream, (long)trailer.PayloadSize, ownsStream: true);
    }

    /// <summary>
    /// Name of the marker file written after successful extraction.
    /// </summary>
    public const string VersionMarkerFileName = ".aspire-bundle-version";

    /// <summary>
    /// Writes a version marker file to the extraction directory.
    /// </summary>
    public static void WriteVersionMarker(string extractDir, ulong versionHash)
    {
        var markerPath = Path.Combine(extractDir, VersionMarkerFileName);
        File.WriteAllText(markerPath, versionHash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Reads the version hash from a previously written marker file.
    /// Returns null if the marker doesn't exist or is invalid.
    /// </summary>
    public static ulong? ReadVersionMarker(string extractDir)
    {
        var markerPath = Path.Combine(extractDir, VersionMarkerFileName);
        if (!File.Exists(markerPath))
        {
            return null;
        }

        var content = File.ReadAllText(markerPath).Trim();
        return ulong.TryParse(content, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var hash) ? hash : null;
    }

    /// <summary>
    /// A stream wrapper that exposes a fixed-length window of an underlying stream.
    /// </summary>
    private sealed class SubStream(Stream inner, long length, bool ownsStream) : Stream
    {
        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = length - _position;
            if (remaining <= 0)
            {
                return 0;
            }

            var toRead = (int)Math.Min(count, remaining);
            var read = inner.Read(buffer, offset, toRead);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPos = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (newPos < 0 || newPos > length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            inner.Seek(newPos - _position, SeekOrigin.Current);
            _position = newPos;
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && ownsStream)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

/// <summary>
/// Information from a parsed bundle trailer.
/// </summary>
internal sealed record BundleTrailerInfo(ulong PayloadOffset, ulong PayloadSize, ulong VersionHash);
