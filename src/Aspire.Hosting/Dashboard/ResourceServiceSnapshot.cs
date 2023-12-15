// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

public sealed class ResourceServiceSnapshot(string name, string? allocatedAddress, int? allocatedPort)
{
    public string Name { get; } = name;
    public string? AllocatedAddress { get; } = allocatedAddress;
    public int? AllocatedPort { get; } = allocatedPort;
}
