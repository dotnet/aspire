// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding hosted agent applications to the distributed application model.
/// </summary>
public static class HostedAgentResourceBuilderExtensions
{
    /// <summary>
    /// Publish the containerized agent as a hosted agent in Azure AI Foundry.
    ///
    /// If a project resource is not provided, the method will attempt to find an existing
    /// Azure Cognitive Services Project resource in the application model. If none exists,
    /// a new project resource (and its parent account resource) will be created automatically.
    /// </summary>
    public static IResourceBuilder<T> PublishAsHostedAgent<T>(
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource> project)
        where T : ExecutableResource
    {
        return PublishAsHostedAgent(builder, project: project);
    }

    /// <summary>
    /// Publish the containerized agent as a hosted agent in Azure AI Foundry.
    ///
    /// If a project resource is not provided, the method will attempt to find an existing
    /// Azure Cognitive Services Project resource in the application model. If none exists,
    /// a new project resource (and its parent account resource) will be created automatically.
    /// </summary>
    public static IResourceBuilder<T> PublishAsHostedAgent<T>(
        this IResourceBuilder<T> builder, Action<HostedAgentConfiguration> configure)
        where T : ExecutableResource
    {
        return PublishAsHostedAgent(builder, project: null, configure: configure);
    }

    /// <summary>
    /// Publish the containerized agent as a hosted agent in Azure AI Foundry.
    ///
    /// If a project resource is not provided, the method will attempt to find an existing
    /// Azure Cognitive Services Project resource in the application model. If none exists,
    /// a new project resource (and its parent account resource) will be created automatically.
    /// </summary>
    public static IResourceBuilder<T> PublishAsHostedAgent<T>(
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource>? project, Action<HostedAgentConfiguration>? configure = null)
        where T : ExecutableResource
    {
        /*
         * Much of the logc here is similar to ExecutableResourceBuilderExtensions.PublishAsDockerFile().
         *
         * That is, in Publish mode, we swap the original resource with a hosted agent resource.
         */
        ArgumentNullException.ThrowIfNull(builder);

        var resource = builder.Resource;
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }
        if (project is null)
        {
            // Try to find an existing project in the model, otherwise create one automatically
            var foundry = builder.ApplicationBuilder.Resources.OfType<AzureAIFoundryResource>().FirstOrDefault();
            if (foundry is not null)
            {
                project = foundry.Project;
            }
            else
            {
                // Build everything
                var foundryBuilder = builder.ApplicationBuilder.AddAzureAIFoundry($"{resource.Name}-foundry");
                project = foundryBuilder.Resource.Project;
            }
        }
        // Hosted Agent resource name
        var agentName = $"{resource.Name}-ha";
        if (builder.ApplicationBuilder.TryCreateResourceBuilder<AzureHostedAgentResource>(agentName, out var rb))
        {
            // We already have a hosted agent for this resource
            if (configure is not null)
            {
                rb.Resource.Configure = configure;
            }
            return builder;
        }
        // Get the corresponding ContainerResource. Usually this is swapped in at publish time for ExecutableResources.
        ContainerResource target;
        if (resource is ContainerResource containerResource)
        {
            target = containerResource;
        }
        else if (builder.ApplicationBuilder.TryCreateResourceBuilder<ContainerResource>(resource.Name, out var crb))
        {
            target = crb.Resource;
        }
        else
        {
            // Ensure we have a container resource to host the agent
            builder.PublishAsDockerFile();
            if (builder.ApplicationBuilder.TryCreateResourceBuilder(resource.Name, out crb))
            {
                target = crb.Resource;
            }
            else
            {
                throw new InvalidOperationException($"Unable to create hosted agent for resource '{resource.Name}' because it could not be converted to a container resource.");
            }
        }
        // Create a separate agent resource to host the deployment
        var agent = new AzureHostedAgentResource(agentName, target, configure);

        // Ensure image gets pushed properly
        target.Annotations.Add(new DeploymentTargetAnnotation(agent)
        {
            ComputeEnvironment = project.Resource,
            ContainerRegistry = project.Resource.GetContainerRegistry()
        });

        builder.ApplicationBuilder.AddResource(agent)
            .WithReferenceRelationship(target)
            .WithReference(project);

        return builder;
    }
}
