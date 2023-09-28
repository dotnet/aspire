// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class ProjectComponent : IDistributedApplicationComponentWithEnvironment
{
    public ComponentMetadataCollection Annotations { get; } = new();

    private string DebuggerToString()
    {
        DistributedApplicationComponentExtensions.TryGetName(this, out var name);
        return $@"Type = {GetType().Name}, Name = ""{name}""";
    }
}
