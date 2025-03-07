// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Docker;

/// <summary>
/// TODO
/// </summary>
public sealed class DockerComposePublisher(IOptions<DockerComposePublisherOptions> options) : IDistributedApplicationPublisher
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        _ = options;
        return Task.CompletedTask;
    }
}