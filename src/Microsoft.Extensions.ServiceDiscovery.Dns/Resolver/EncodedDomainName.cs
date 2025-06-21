// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal struct EncodedDomainName : IEquatable<EncodedDomainName>, IDisposable
{
    public IReadOnlyList<ReadOnlyMemory<byte>> Labels { get; }
    private byte[]? _pooledBuffer;

    public EncodedDomainName(List<ReadOnlyMemory<byte>> labels, byte[]? pooledBuffer = null)
    {
        Labels = labels;
        _pooledBuffer = pooledBuffer;
    }
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        foreach (var label in Labels)
        {
            if (sb.Length > 0)
            {
                sb.Append('.');
            }
            sb.Append(Encoding.ASCII.GetString(label.Span));
        }

        return sb.ToString();
    }

    public bool Equals(EncodedDomainName other)
    {
        if (Labels.Count != other.Labels.Count)
        {
            return false;
        }

        for (int i = 0; i < Labels.Count; i++)
        {
            if (!Ascii.EqualsIgnoreCase(Labels[i].Span, other.Labels[i].Span))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EncodedDomainName other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        foreach (var label in Labels)
        {
            foreach (byte b in label.Span)
            {
                hash.Add((byte)char.ToLower((char)b));
            }
        }

        return hash.ToHashCode();
    }

    public void Dispose()
    {
        if (_pooledBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_pooledBuffer);
        }

        _pooledBuffer = null;
    }
}