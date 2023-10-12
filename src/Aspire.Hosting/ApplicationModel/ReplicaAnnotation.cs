// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Replicas = {Replicas}")]
public sealed class ReplicaAnnotation : IDistributedApplicationResourceAnnotation
{
    public ReplicaAnnotation(int replicas = 1)
    {
        Replicas = replicas;
    }

    public int Replicas { get; private set; }
}
