// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Name = {Name}")]
public class ServiceBinding
{
    public string? Name { get; set; }
    public string? ConnectionString { get; set; }
    public int? Port { get; set; }
    public int? ContainerPort { get; set; }
    public string? Host { get; set; }
    public string? Address { get; set; }
    public string? Protocol { get; set; }
    public List<int> ReplicaPorts { get; } = new List<int>();
    public List<string> Routes { get; } = new List<string>();
}
