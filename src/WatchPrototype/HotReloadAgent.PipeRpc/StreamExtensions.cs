// Copyright (c) Microsoft Corporation. All rights reserved.

#nullable enable

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Implements async read/write helpers that provide functionality of <see cref="BinaryReader"/> and <see cref="BinaryWriter"/>.
/// See https://github.com/dotnet/runtime/issues/17229
/// </summary>
internal static class StreamExtesions
{
    public static ValueTask WriteAsync(this Stream stream, bool value, CancellationToken cancellationToken)
        => WriteAsync(stream, (byte)(value ? 1 : 0), cancellationToken);

    public static async ValueTask WriteAsync(this Stream stream, byte value, CancellationToken cancellationToken)
    {
        var size = sizeof(byte);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            buffer[0] = value;
            await stream.WriteAsync(buffer, offset: 0, count: size, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask WriteAsync(this Stream stream, int value, CancellationToken cancellationToken)
    {
        var size = sizeof(int);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            await stream.WriteAsync(buffer, offset: 0, count: size, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static ValueTask WriteAsync(this Stream stream, Guid value, CancellationToken cancellationToken)
        => stream.WriteAsync(value.ToByteArray(), cancellationToken);

    public static async ValueTask WriteByteArrayAsync(this Stream stream, byte[] value, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(value.Length, cancellationToken);
        await stream.WriteAsync(value, cancellationToken);
    }

    public static async ValueTask WriteAsync(this Stream stream, int[] value, CancellationToken cancellationToken)
    {
        var size = sizeof(int) * (value.Length + 1);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan((i + 1) * sizeof(int), sizeof(int)), value[i]);
            }

            await stream.WriteAsync(buffer, offset: 0, count: size, cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask WriteAsync(this Stream stream, string value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        await stream.Write7BitEncodedIntAsync(bytes.Length, cancellationToken);
        await stream.WriteAsync(bytes, cancellationToken);
    }

#if !NET
        public static async ValueTask WriteAsync(this Stream stream, byte[] value, CancellationToken cancellationToken)
            => await stream.WriteAsync(value, offset: 0, count: value.Length, cancellationToken);
#endif
    public static async ValueTask Write7BitEncodedIntAsync(this Stream stream, int value, CancellationToken cancellationToken)
    {
        uint uValue = (uint)value;

        while (uValue > 0x7Fu)
        {
            await stream.WriteAsync((byte)(uValue | ~0x7Fu), cancellationToken);
            uValue >>= 7;
        }

        await stream.WriteAsync((byte)uValue, cancellationToken);
    }

    public static async ValueTask<bool> ReadBooleanAsync(this Stream stream, CancellationToken cancellationToken)
        => await stream.ReadByteAsync(cancellationToken) != 0;

    public static async ValueTask<byte> ReadByteAsync(this Stream stream, CancellationToken cancellationToken)
    {
        int size = sizeof(byte);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);
            return buffer[0];
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask<int> ReadInt32Async(this Stream stream, CancellationToken cancellationToken)
    {
        int size = sizeof(int);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask<Guid> ReadGuidAsync(this Stream stream, CancellationToken cancellationToken)
    {
        const int size = 16;
#if NET
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);

        try
        {
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);
            return new Guid(buffer.AsSpan(0, size));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#else
            var buffer = new byte[size];
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);
            return new Guid(buffer);
#endif
    }

    public static async ValueTask<byte[]> ReadByteArrayAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var count = await stream.ReadInt32Async(cancellationToken);
        if (count == 0)
        {
            return [];
        }

        var bytes = new byte[count];
        await ReadExactlyAsync(stream, bytes, count, cancellationToken);
        return bytes;
    }

    public static async ValueTask<int[]> ReadIntArrayAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var count = await stream.ReadInt32Async(cancellationToken);
        if (count == 0)
        {
            return [];
        }

        var result = new int[count];
        int size = count * sizeof(int);
        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);

            for (var i = 0; i < count; i++)
            {
                result[i] = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(i * sizeof(int)));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return result;
    }

    public static async ValueTask<string> ReadStringAsync(this Stream stream, CancellationToken cancellationToken)
    {
        int size = await stream.Read7BitEncodedIntAsync(cancellationToken);
        if (size < 0)
        {
            throw new InvalidDataException();
        }

        if (size == 0)
        {
            return string.Empty;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(minimumLength: size);
        try
        {
            await ReadExactlyAsync(stream, buffer, size, cancellationToken);
            return Encoding.UTF8.GetString(buffer, 0, size);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static async ValueTask<int> Read7BitEncodedIntAsync(this Stream stream, CancellationToken cancellationToken)
    {
        const int MaxBytesWithoutOverflow = 4;

        uint result = 0;
        byte b;

        for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            b = await stream.ReadByteAsync(cancellationToken);
            result |= (b & 0x7Fu) << shift;

            if (b <= 0x7Fu)
            {
                return (int)result;
            }
        }

        // Read the 5th byte. Since we already read 28 bits,
        // the value of this byte must fit within 4 bits (32 - 28),
        // and it must not have the high bit set.

        b = await stream.ReadByteAsync(cancellationToken);
        if (b > 0b_1111u)
        {
            throw new InvalidDataException();
        }

        result |= (uint)b << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    private static async ValueTask<int> ReadExactlyAsync(this Stream stream, byte[] buffer, int size, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < size)
        {
            int read = await stream.ReadAsync(buffer, offset: totalRead, count: size - totalRead, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            totalRead += read;
        }

        return totalRead;
    }
}
