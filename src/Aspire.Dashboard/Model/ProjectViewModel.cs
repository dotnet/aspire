// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

/// <summary>
/// Immutable snapshot of project state at a point in time.
/// </summary>
public class ProjectViewModel : ExecutableViewModel
{
    public override string ResourceType => "Project";

    public required string ProjectPath { get; init; }
}
