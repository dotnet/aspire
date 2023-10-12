// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class DistributedApplicationResource(string name) : IDistributedApplicationResource
{
    public string Name { get; } = name;
    public ResourceMetadataCollection Annotations { get; } = new ResourceMetadataCollection();

    private string DebuggerToString()
    {
        return $@"Type = {GetType().Name}, Name = ""{Name}""";
    }
}
