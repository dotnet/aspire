// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Keep track of instances from DCP.
/// </summary>
internal class DcpInstancesAnnotation(ImmutableArray<DcpInstance> instances) : IResourceAnnotation
{
    public ImmutableArray<DcpInstance> Instances => instances;
}

internal sealed record DcpInstance(string Name, string Suffix, int Index);
