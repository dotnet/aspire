// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class SpanCollection : KeyedCollection<string, OtlpSpan>
{
    // Chosen to balance memory usage overhead of dictionary vs work to find spans by ID.
    private const int DictionaryCreationThreshold = 128;

    public SpanCollection() : base(StringComparers.OtlpSpanId, DictionaryCreationThreshold)
    {
    }

    protected override string GetKeyForItem(OtlpSpan item)
    {
        return item.SpanId;
    }

    public new List<OtlpSpan>.Enumerator GetEnumerator()
    {
        return ((List<OtlpSpan>)this.Items).GetEnumerator();
    }
}
