// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that specifies the number of replicas for a resource.
/// </summary>
/// <param name="replicas">The number of replicas for the resource.</param>
[DebuggerDisplay("Type = {GetType().Name,nq}, Replicas = {Replicas}")]
public sealed class ReplicaAnnotation(int replicas = 1) : IResourceAnnotation
{
    /// <summary>
    /// Gets the number of replicas for the application.
    /// </summary>
    public int Replicas { get; } = replicas;
}
