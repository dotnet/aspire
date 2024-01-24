// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Immutable snapshot of a project's state at a point in time.
/// </summary>
internal class ProjectSnapshot : ExecutableSnapshot
{
    public override string ResourceType => KnownResourceTypes.Project;

    public required string ProjectPath { get; init; }

    protected override IEnumerable<(string Key, Value Value)> GetProperties()
    {
        yield return (KnownProperties.Project.Path, Value.ForString(ProjectPath));

        foreach (var pair in base.GetProperties())
        {
            yield return pair;
        }
    }
}
