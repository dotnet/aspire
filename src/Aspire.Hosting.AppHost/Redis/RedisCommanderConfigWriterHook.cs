// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Text;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Redis;

internal class RedisCommanderConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        if (appModel.Resources.OfType<RedisCommanderResource>().SingleOrDefault() is not { } commanderResource)
        {
            // No-op if there is no commander resource (removed after hook added).
            return Task.CompletedTask;
        }

        var redisInstances = appModel.Resources.OfType<RedisResource>();

        if (!redisInstances.Any())
        {
            // No-op if there are no Redis resources present.
            return Task.CompletedTask;
        }

        var hostsVariableBuilder = new StringBuilder();

        foreach (var redisInstance in redisInstances)
        {
            if (redisInstance.PrimaryEndpoint.IsAllocated)
            {
                var hostString = $"{(hostsVariableBuilder.Length > 0 ? "," : string.Empty)}{redisInstance.Name}:{redisInstance.PrimaryEndpoint.ContainerHost}:{redisInstance.PrimaryEndpoint.Port}:0";
                hostsVariableBuilder.Append(hostString);
            }
        }

        commanderResource.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
        {
            context.EnvironmentVariables.Add("REDIS_HOSTS", hostsVariableBuilder.ToString());
        }));

        return Task.CompletedTask;
    }
}
