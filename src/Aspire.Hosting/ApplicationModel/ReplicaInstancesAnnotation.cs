// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Keep track of instances from DCP.
/// </summary>
internal class ReplicaInstancesAnnotation : IResourceAnnotation
{
    public ConcurrentDictionary<string, Instance> Instances { get; } = [];
}

internal sealed record Instance(string Name, string Suffix, int Index);
