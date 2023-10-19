// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ResourceSelectItem<TResource> : IEquatable<ResourceSelectItem<TResource>> where TResource : ResourceViewModel
{
    public required string Text { get; set; }
    public TResource? Resource { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as ResourceSelectItem<TResource>;
        return Equals(other);
    }

    public bool Equals(ResourceSelectItem<TResource>? other)
    {
        return Resource?.Uid == other?.Resource?.Uid;
    }

    public override int GetHashCode()
    {
        return Resource?.Uid.GetHashCode() ?? 0;
    }
}
