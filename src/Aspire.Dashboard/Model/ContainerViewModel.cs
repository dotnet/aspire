// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ContainerViewModel
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public string? ContainerID { get; init; }
    public DateTime? CreationTimeStamp { get; init; }
    public required string Image { get; init; }
    public List<int> Ports { get; } = new();
    public required IContainerLogSource LogSource { get; init; }
    public List<EnvironmentVariableViewModel> Environment { get; } = new();
}
