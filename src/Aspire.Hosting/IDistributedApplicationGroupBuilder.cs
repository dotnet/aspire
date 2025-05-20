// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// TODO document this type
/// </summary>
public interface IDistributedApplicationGroupBuilder : IDistributedApplicationBuilder
{
    /// <summary>
    /// The unique name of the group. This name is used to identify the group in the Aspire Dashboard.
    /// </summary>
    public string Name { get; }

    internal void BuildGroup();

    [Obsolete("Use BuildGroup instead.")]
    internal new DistributedApplication Build();
}
