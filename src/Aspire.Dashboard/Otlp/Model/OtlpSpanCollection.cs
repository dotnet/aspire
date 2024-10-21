// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Dashboard.Otlp.Model;

public sealed class OtlpSpanCollection : KeyedCollection<string, OtlpSpan>
{
    // Chosen to balance memory usage overhead of dictionary vs work to find spans by ID.
    private const int DictionaryCreationThreshold = 128;

    public OtlpSpanCollection() : base(StringComparers.OtlpSpanId, DictionaryCreationThreshold)
    {
    }

    protected override string GetKeyForItem(OtlpSpan item)
    {
        return item.SpanId;
    }

    public new List<OtlpSpan>.Enumerator GetEnumerator()
    {
        // Avoid allocating an enumerator when iterated with foreach.
        return ((List<OtlpSpan>)this.Items).GetEnumerator();
    }
}
