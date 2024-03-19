// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

/// <summary>
/// A resource that represents a Redis Commander container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisCommanderResource(string name) : ContainerResource(name)
{
}
