// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Orleans;

/// <summary>
/// Represents an Orleans client.
/// </summary>
/// <param name="service">The Orleans service which this client connects to.</param>
public sealed class OrleansServiceClient(OrleansService service)
{
    /// <summary>
    /// Gets the Orleans service which this client will connect to.
    /// </summary>
    public OrleansService Service { get; } = service;
}
