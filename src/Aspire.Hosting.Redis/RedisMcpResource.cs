// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

/// <summary>
/// A resource representing the Redis MCP sidecar.
/// </summary>
public class RedisMcpResource(string name) : ContainerResource(name);
