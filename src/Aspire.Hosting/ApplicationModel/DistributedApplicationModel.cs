// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Name = {Name}, Components = {Components.Count}")]
public class DistributedApplicationModel(IDistributedApplicationComponentCollection components)
{
    public IDistributedApplicationComponentCollection Components { get; } = components;

    public string? Name { get; set; }
}
