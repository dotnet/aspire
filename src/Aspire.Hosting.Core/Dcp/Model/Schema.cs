// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp.Model;

internal sealed class Schema
{
    private readonly Dictionary<Type, (string Kind, string Resource)> _byType = new();

    public void Add<T>(string kind, string resource) where T : CustomResource
    {
        _byType.Add(typeof(T), (kind, resource));
    }

    public bool TryGet<T>(out (string Kind, string Resource) kindWithResource) where T : CustomResource
    {
        return _byType.TryGetValue(typeof(T), out kindWithResource);
    }
}
