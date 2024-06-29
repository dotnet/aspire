// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils.Cache;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Valkey resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ValkeyResource(string name) : CacheResource(name);
