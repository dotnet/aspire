// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// TODO document this type
/// </summary>
public interface IDistributedApplicationGroupBuilder : IDistributedApplicationBuilder
{
    internal void BuildGroup();

    [Obsolete("Use BuildGroup instead.")]
    internal new DistributedApplication Build();
}
