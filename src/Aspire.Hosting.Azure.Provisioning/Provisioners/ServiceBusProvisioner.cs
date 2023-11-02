// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure.Provisioning;

internal sealed class ServiceBusProvisioner(ILogger<ServiceBusProvisioner> logger) : AzureResourceProvisioner<AzureServiceBusResource>
{
    public override bool ConfigureResource(IConfiguration configuration, AzureServiceBusResource resource)
    {
        if (configuration.GetConnectionString(resource.Name) is string endpoint)
        {
            resource.ServiceBusEndpoint = endpoint;
            return true;
        }

        return false;
    }

    public override async Task GetOrCreateResourceAsync(
        ArmClient armClient,
        SubscriptionResource subscription,
        ResourceGroupResource resourceGroup,
        Dictionary<string, ArmResource> resourceMap,
        AzureLocation location,
        AzureServiceBusResource resource,
        Guid principalId,
        JsonObject userSecrets,
        CancellationToken cancellationToken)
    {
        resourceMap.TryGetValue(resource.Name, out var azureResource);

        if (azureResource is not null && azureResource is not ServiceBusNamespaceResource)
        {
            logger.LogWarning("Resource {resourceName} is not a service bus namespace. Deleting it.", resource.Name);

            await armClient.GetGenericResource(azureResource.Id).DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
        }

        var serviceBusNamespace = azureResource as ServiceBusNamespaceResource;

        if (serviceBusNamespace is null)
        {
            logger.LogInformation("Creating service bus namespace in {location}...", location);

            var attempts = 0;

            while (true)
            {
                try
                {
                    // ^[a-zA-Z][a-zA-Z0-9-]*$
                    var namespaceName = Guid.NewGuid().ToString();

                    var parameters = new ServiceBusNamespaceData(location);
                    parameters.Tags.Add(AzureProvisioner.AspireResourceNameTag, resource.Name);

                    // Now we can create a storage account with defined account name and parameters
                    var operation = await resourceGroup.GetServiceBusNamespaces().CreateOrUpdateAsync(WaitUntil.Completed, namespaceName, parameters, cancellationToken).ConfigureAwait(false);
                    serviceBusNamespace = operation.Value;

                    // Success
                    break;
                }
                catch (RequestFailedException)
                {
                    // We've seen errors like
                    // "The specified service namespace is invalid" when we know that guids are valid
                    // service bus namespace names.
                    if (attempts++ > 3)
                    {
                        throw;
                    }

                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }
            }

            logger.LogInformation("Service bus namespace {namespace} created.", serviceBusNamespace.Data.Name);
        }

        // This is the full uri to the service bus namespace e.g https://namespace.servicebus.windows.net:443/
        // the connection strings for the app need the host name only
        resource.ServiceBusEndpoint = new Uri(serviceBusNamespace.Data.ServiceBusEndpoint).Host;

        var connectionStrings = userSecrets.Prop("ConnectionStrings");
        connectionStrings[resource.Name] = resource.ServiceBusEndpoint;

        // Now create the queues
        var queues = serviceBusNamespace.GetServiceBusQueues();
        var topics = serviceBusNamespace.GetServiceBusTopics();

        var queuesToCreate = new HashSet<string>(resource.QueueNames);
        var topicsToCreate = new HashSet<string>(resource.TopicNames);

        // Delete unused queues
        await foreach (var sbQueue in queues.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (!resource.QueueNames.Contains(sbQueue.Data.Name))
            {
                logger.LogInformation("Deleting queue {queueName}", sbQueue.Data.Name);

                await sbQueue.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }

            // Don't need to create this queue
            queuesToCreate.Remove(sbQueue.Data.Name);
        }

        await foreach (var sbTopic in topics.GetAllAsync(cancellationToken: cancellationToken))
        {
            if (!resource.TopicNames.Contains(sbTopic.Data.Name))
            {
                logger.LogInformation("Deleting topic {topicName}", sbTopic.Data.Name);

                await sbTopic.DeleteAsync(WaitUntil.Started, cancellationToken).ConfigureAwait(false);
            }

            // Don't need to create this topic
            topicsToCreate.Remove(sbTopic.Data.Name);
        }

        // Create the remaining queues
        foreach (var queueName in queuesToCreate)
        {
            logger.LogInformation("Creating queue {queueName}...", queueName);

            await queues.CreateOrUpdateAsync(WaitUntil.Completed, queueName, new ServiceBusQueueData(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Queue {queueName} created.", queueName);
        }

        // Create the remaining topics
        foreach (var topicName in topicsToCreate)
        {
            logger.LogInformation("Creating topic {topicName}...", topicName);

            await topics.CreateOrUpdateAsync(WaitUntil.Completed, topicName, new ServiceBusTopicData(), cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Topic {topicName} created.", topicName);
        }

        // Azure Service Bus Data Owner
        // https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#azure-service-bus-data-owner
        var roleDefinitionId = CreateRoleDefinitionId(subscription, "090c5cfd-751d-490a-894a-3ce6f1109419");

        await DoRoleAssignmentAsync(armClient, serviceBusNamespace.Id, principalId, roleDefinitionId, cancellationToken).ConfigureAwait(false);
    }
}
