// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Garnet;

internal sealed class RedisCommanderConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        if (appModel.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is not { } commanderResource)
        {
            // No-op if there is no commander resource (removed after hook added).
            return Task.CompletedTask;
        }

        var garnetInstances = appModel.Resources.OfType<GarnetResource>();

        if (!garnetInstances.Any())
        {
            // No-op if there are no Garnet resources present.
            return Task.CompletedTask;
        }

        var hostsVariableBuilder = new StringBuilder();

        foreach (var garnetInstance in garnetInstances)
        {
            if (garnetInstance.PrimaryEndpoint.IsAllocated)
            {
                var hostString = $"{(hostsVariableBuilder.Length > 0 ? "," : string.Empty)}{garnetInstance.Name}:{garnetInstance.PrimaryEndpoint.ContainerHost}:{garnetInstance.PrimaryEndpoint.Port}:0";
                hostsVariableBuilder.Append(hostString);
            }
        }

        commanderResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables.Add("GARNET_HOSTS", hostsVariableBuilder.ToString());
        }));

        return Task.CompletedTask;
    }
}
