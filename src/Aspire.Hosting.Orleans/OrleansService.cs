// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Orleans;

/// <summary>
/// Describes an Orleans service.
/// </summary>
/// <param name="builder">The distributed application builder.</param>
/// <param name="name">The service name.</param>
public class OrleansService(IDistributedApplicationBuilder builder, string name)
{
    /// <summary>
    /// Gets the name of the service.
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
    public IProviderConfiguration? Clustering { get; set; }

    /// <summary>
    /// Gets or sets the reminder service provider.
    /// </summary>
    public IProviderConfiguration? Reminders { get; set; }

    /// <summary>
    /// Gets the grain storage providers.
    /// </summary>
    public Dictionary<string, IProviderConfiguration> GrainStorage { get; } = [];

    /// <summary>
    /// Gets the grain directory providers.
    /// </summary>
    public Dictionary<string, IProviderConfiguration> GrainDirectory { get; } = [];

    /// <summary>
    /// Gets the broadcast channel providers.
    /// </summary>
    public Dictionary<string, IProviderConfiguration> BroadcastChannel { get; } = [];

    /// <summary>
    /// Gets the stream providers.
    /// </summary>
    public Dictionary<string, IProviderConfiguration> Streaming { get; } = [];
}
