// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Describes an Orleans cluster.
/// </summary>
/// <param name="builder">The distributed application builder.</param>
/// <param name="name">The cluster name.</param>
public class OrleansCluster(IDistributedApplicationBuilder builder, string name)
{
    /// <summary>
    /// Gets the name of the cluster.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the distributed application builder.
    /// </summary>
    public IDistributedApplicationBuilder Builder { get; } = builder;

    /// <summary>
    /// Gets or sets the service identifier.
    /// </summary>
    public string? ServiceId { get; set; }

    /// <summary>
    /// Gets or sets the cluster identifier.
    /// </summary>
    public string? ClusterId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the clustering provider.
    /// </summary>
    public object? Clustering { get; set; }

    /// <summary>
    /// Gets or sets the reminder service provider.
    /// </summary>
    public object? Reminders { get; set; }

    /// <summary>
    /// Gets the grain storage providers.
    /// </summary>
    public Dictionary<string, object> GrainStorage { get; } = [];
}
