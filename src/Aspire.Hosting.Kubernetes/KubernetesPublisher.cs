// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// TODO
/// </summary>
internal sealed class KubernetesPublisher([ServiceKey]string name, IOptionsMonitor<KubernetesPublisherOptions> options) : IDistributedApplicationPublisher
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        _ = options.Get(name);
        return Task.CompletedTask;
    }
}