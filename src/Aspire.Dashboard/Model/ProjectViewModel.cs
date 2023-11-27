// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ProjectViewModel : ResourceViewModel
{
    public override string ResourceType => "Project";
    public int? ProcessId { get; init; }
    public required string ProjectPath { get; init; }
}
