// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Docker;

internal sealed class ResourceComparer : IEqualityComparer<IResource>
{
    public bool Equals(IResource? x, IResource? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Name.Equals(y.Name, StringComparison.Ordinal);
    }

    public int GetHashCode(IResource obj) =>
        obj.Name.GetHashCode(StringComparison.Ordinal);
}
