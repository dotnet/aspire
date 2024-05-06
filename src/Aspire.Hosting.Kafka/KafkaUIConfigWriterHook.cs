// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Kafka;

internal sealed class KafkaUIConfigurationHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var kafkaUIResource = appModel.Resources.OfType<KafkaUIContainerResource>().Single();
        var kafkaResources = appModel.Resources.OfType<KafkaServerResource>();

        int i = 0;
        foreach (var kafkaResource in kafkaResources)
        {
            if (kafkaResource.InternalEndpoint.IsAllocated)
            {
                var endpoint = kafkaResource.InternalEndpoint;
                int index = i;
                kafkaUIResource.Annotations.Add(new EnvironmentCallbackAnnotation(context => ConfigureKafkaUIContainer(context, endpoint, index)));
            }

            i++;
        }

        return Task.CompletedTask;
    }

    private static void ConfigureKafkaUIContainer(EnvironmentCallbackContext context, EndpointReference endpoint, int index)
    {
        var bootstrapServers = context.ExecutionContext.IsRunMode
            ? ReferenceExpression.Create($"{endpoint.ContainerHost}:{endpoint.Property(EndpointProperty.Port)}")
            : ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");

        context.EnvironmentVariables.Add($"KAFKA_CLUSTERS_{index}_NAME", endpoint.Resource.Name);
        context.EnvironmentVariables.Add($"KAFKA_CLUSTERS_{index}_BOOTSTRAPSERVERS", bootstrapServers);
    }
}
