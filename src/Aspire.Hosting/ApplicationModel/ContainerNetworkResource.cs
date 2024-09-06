// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified container network.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ContainerNetworkResource(string name) : Resource(name)
{}