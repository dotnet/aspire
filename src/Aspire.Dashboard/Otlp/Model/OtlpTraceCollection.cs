// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class OtlpTraceCollection : KeyedCollection<ReadOnlyMemory<byte>, OtlpTrace>
{
    public OtlpTraceCollection() : base(MemoryComparable.Instance, dictionaryCreationThreshold: 0)
    {

    }

    protected override ReadOnlyMemory<byte> GetKeyForItem(OtlpTrace item)
    {
        return item.Key;
    }

    private sealed class MemoryComparable : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public static readonly MemoryComparable Instance = new();

        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            return x.Span.SequenceEqual(y.Span);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            unchecked
            {
                var hash = 17;
                foreach (var value in obj.Span)
                {
                    hash = hash * 23 + value.GetHashCode();
                }
                return hash;
            }
        }
    }
}
