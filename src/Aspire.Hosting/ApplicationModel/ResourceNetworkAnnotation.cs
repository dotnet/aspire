// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which represents the resource being attached to a network.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Network = {Network.Name}")]
public sealed class ResourceNetworkAnnotation(NetworkResource network) : IResourceAnnotation
{
    /// <summary>
    /// The network the resource is attached to.
    /// </summary>
    public NetworkResource Network { get; } = network;
}
