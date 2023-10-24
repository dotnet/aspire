// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public abstract class ResourceViewModel
{
    public required string Name { get; init; }
    public required NamespacedName NamespacedName { get; init; }
    public string? State { get; init; }
    public DateTime? CreationTimeStamp { get; init; }
    public List<EnvironmentVariableViewModel> Environment { get; } = new();
    public required ILogSource LogSource { get; init; }
    public List<string> Endpoints { get; } = new();
    public int? ExpectedEndpointsCount { get; init; }
}

public sealed record NamespacedName(string Name, string? Namespace);
