// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard;

internal sealed class MockKnownPropertyLookup() : IKnownPropertyLookup
{
    private int _priority = int.MaxValue;
    private KnownProperty? _knownProperty;

    public MockKnownPropertyLookup(int priority, KnownProperty? knownProperty) : this()
    {
        _priority = priority;
        _knownProperty = knownProperty;
    }

    public void Set(int priority, KnownProperty? knownProperty)
    {
        _priority = priority;
        _knownProperty = knownProperty;
    }

    public (int priority, KnownProperty? knownProperty) FindProperty(string resourceType, string uid)
    {
        return (_priority, _knownProperty);
    }
}
