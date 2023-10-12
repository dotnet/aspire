// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ProjectViewModel : ResourceViewModel
{
    public required string ProjectPath { get; init; }
    public List<string> Endpoints { get; } = new();
    public required int ExpectedEndpointsCount { get; init; }
}
