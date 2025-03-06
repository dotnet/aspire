// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Docker;

/// <summary>
/// TODO
/// </summary>
public sealed class DockerComposePublisher : IDistributedApplicationPublisher
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}