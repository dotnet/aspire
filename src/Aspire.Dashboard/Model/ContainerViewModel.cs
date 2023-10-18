// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ContainerViewModel : ResourceViewModel
{
    public string? ContainerId { get; init; }
    public required string Image { get; init; }
    public List<int> Ports { get; } = new();
}
