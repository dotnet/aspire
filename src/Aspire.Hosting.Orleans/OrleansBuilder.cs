// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class OrleansBuilder(IDistributedApplicationBuilder builder, string name)
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public string Name { get; } = name;

    public IDistributedApplicationBuilder Builder { get; } = builder;
    public string? ServiceId { get; set; }
    public string? ClusterId { get; set; } = Guid.NewGuid().ToString("N");
    public object? Clustering { get; set; }
    public IResourceBuilder<IResourceWithConnectionString>? Reminders { get; set; }
    public Dictionary<string, object> GrainStorage { get; } = new();
}
