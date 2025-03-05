// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver.Tests;

public class DnsPrimitivesTests
{
    public static TheoryData<string, byte[]> QNameData => new()
    {
        { "www.example.com", "\x0003www\x0007example\x0003com\x0000"u8.ToArray() },
        { "example.com", "\x0007example\x0003com\x0000"u8.ToArray() },
        { "com", "\x0003com\x0000"u8.ToArray() },
        { "example", "\x0007example\x0000"u8.ToArray() },
        { "www", "\x0003www\x0000"u8.ToArray() },
        { "a", "\x0001a\x0000"u8.ToArray() },
    };

    [Theory]
    [MemberData(nameof(QNameData))]
    public void TryWriteQName_Success(string name, byte[] expected)
    {
        byte[] buffer = new byte[512];

        Assert.True(DnsPrimitives.TryWriteQName(buffer, name, out int written));
        Assert.Equal(name.Length + 2, written);
        Assert.Equal(expected, buffer.AsSpan().Slice(0, written).ToArray());
    }

    [Fact]
    public void TryWriteQName_LabelTooLong_Throws()
    {
        byte[] buffer = new byte[512];

        Assert.Throws<ArgumentException>(() => DnsPrimitives.TryWriteQName(buffer, new string('a', 70), out int written));
    }

    [Fact]
    public void TryWriteQName_BufferTooShort_Fails()
    {
        byte[] buffer = new byte[512];
        string name = "www.example.com";

        for (int i = 0; i < name.Length + 2; i++)
        {
            Assert.False(DnsPrimitives.TryWriteQName(buffer.AsSpan(0, i), name, out _));
        }
    }

    [Theory]
    [MemberData(nameof(QNameData))]
    public void TryReadQName_Success(string expected, byte[] serialized)
    {
        Assert.True(DnsPrimitives.TryReadQName(serialized, 0, out string? actual, out int bytesRead));
        Assert.Equal(expected, actual);
        Assert.Equal(serialized.Length, bytesRead);
    }

    [Fact]
    public void TryReadQName_TruncatedData_Fails()
    {
        ReadOnlySpan<byte> data = "\x0003www\x0007example\x0003com\x0000"u8;

        for (int i = 0; i < data.Length; i++)
        {
            Assert.False(DnsPrimitives.TryReadQName(data.Slice(0, i), 0, out _, out _));
        }
    }

    [Fact]
    public void TryReadQName_Pointer_Success()
    {
        // [7B padding], example.com. www->[ptr to example.com.]
        Span<byte> data = "padding\x0007example\x0003com\x0000\x0003www\x00\x07"u8.ToArray();
        data[^2] = 0xc0;

        Assert.True(DnsPrimitives.TryReadQName(data, data.Length - 6, out string? actual, out int bytesRead));
        Assert.Equal("www.example.com", actual);
        Assert.Equal(6, bytesRead);
    }

    [Fact]
    public void TryReadQName_PointerTruncated_Fails()
    {
        // [7B padding], example.com. www->[ptr to example.com.]
        Span<byte> data = "padding\x0007example\x0003com\x0000\x0003www\x00\x07"u8.ToArray();
        data[^2] = 0xc0;

        for (int i = 0; i < data.Length; i++)
        {
            Assert.False(DnsPrimitives.TryReadQName(data.Slice(0, i), data.Length - 6, out _, out _));
        }
    }

    [Fact]
    public void TryReadQName_ForwardPointer_Fails()
    {
        // www->[ptr to example.com], [7B padding], example.com.
        Span<byte> data = "\x03www\x00\x000dpadding\x0007example\x0003com\x00"u8.ToArray();
        data[4] = 0xc0;

        Assert.False(DnsPrimitives.TryReadQName(data, 0, out _, out _));
    }

    [Fact]
    public void TryReadQName_PointerToSelf_Fails()
    {
        // www->[ptr to www->...]
        Span<byte> data = "\x0003www\0\0"u8.ToArray();
        data[4] = 0xc0;

        Assert.False(DnsPrimitives.TryReadQName(data, 0, out _, out _));
    }

    [Fact]
    public void TryReadQName_PointerToPointer_Fails()
    {
        // com, example[->com], example2[->[->com]]
        Span<byte> data = "\x0003com\0\x0007example\0\0\x0008example2\0\0"u8.ToArray();
        data[13] = 0xc0;
        data[14] = 0x00; // -> com
        data[24] = 0xc0;
        data[25] = 13; // -> -> com

        Assert.False(DnsPrimitives.TryReadQName(data, 15, out _, out _));
    }

    [Fact]
    public void TryReadQName_ReservedBits()
    {
        Span<byte> data = "\x0003www\x00c0"u8.ToArray();
        data[0] = 0x40;

        Assert.False(DnsPrimitives.TryReadQName(data, 0, out _, out _));
    }

    [Theory]
    [InlineData(253)]
    [InlineData(254)]
    [InlineData(255)]
    public void TryReadQName_NameTooLong(int length)
    {
        // longest possible label is 63 bytes + 1 byte for length
        byte[] labelData = new byte[64];
        Array.Fill(labelData, (byte)'a');
        labelData[0] = 63;

        int remainder = length - 3 * 64;

        byte[] lastLabelData = new byte[remainder + 1];
        Array.Fill(lastLabelData, (byte)'a');
        lastLabelData[0] = (byte)remainder;

        byte[] data = Enumerable.Repeat(labelData, 3).SelectMany(x => x).Concat(lastLabelData).Concat(new byte[1]).ToArray();
        if (length > 253)
        {
            Assert.False(DnsPrimitives.TryReadQName(data, 0, out _, out _));
        }
        else
        {
            Assert.True(DnsPrimitives.TryReadQName(data, 0, out _, out _));
        }
    }
}
