// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// A builder for creating instances of resource groupings. It cannot be built directly, but applies annotations to
/// its contained resources when <see cref="IDistributedApplicationBuilder.Build"/> is called. This type can be used to
/// visually group resources in the Aspire Dashboard.
/// </summary>
public interface IDistributedApplicationGroupBuilder : IDistributedApplicationBuilder
{
    /// <summary>
    /// The unique name of the group. This name is used to identify the group in the Aspire Dashboard.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Applies annotations to resources in the group.
    /// </summary>
    internal void BuildGroup();

    [Obsolete("Use BuildGroup instead.")]
    internal new DistributedApplication Build();
}
