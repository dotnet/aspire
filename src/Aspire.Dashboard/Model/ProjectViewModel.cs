// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ProjectViewModel
{
    public required string Name { get; init; }
    public string? State { get; init; }
    public DateTime? CreationTimeStamp { get; init; }

    public required string ProjectPath { get; init; }

    public List<string> Addresses { get; } = new();

    public List<ServiceEndpoint> Endpoints { get; } = new();
    public List<EnvironmentVariableViewModel> Environment { get; } = new();
    public required IProjectLogSource LogSource { get; init; }
    public required int ExpectedEndpointCount { get; init; }
}

