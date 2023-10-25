// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Name = {Name}, Resources = {Resources.Count}")]
public class DistributedApplicationModel(IResourceCollection resources)
{
    public IResourceCollection Resources { get; } = resources;

    public string? Name { get; set; }
}
