// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a distributed application.
/// </summary>
/// <param name="resources">The resource collection used to initiate the model.</param>
/// <param name="applicationBuilder">The <see cref="IDistributedApplicationBuilder" /> that is attached to the model.</param>
[DebuggerDisplay("Resources = {Resources.Count}")]
public class DistributedApplicationModel(IResourceCollection resources, IDistributedApplicationBuilder applicationBuilder)
{
    /// <summary>
    /// Gets the collection of resources associated with the distributed application.
    /// </summary>
    public IResourceCollection Resources { get; } = resources ?? throw new ArgumentNullException(nameof(resources));

    /// <summary>
    /// The <see cref="IDistributedApplicationBuilder"/> that is attached to the model.
    /// </summary>
    public IDistributedApplicationBuilder ApplicationBuilder { get; } = applicationBuilder;
}
