// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

internal sealed class ResourceNameComparer : IEqualityComparer<IResource>
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
