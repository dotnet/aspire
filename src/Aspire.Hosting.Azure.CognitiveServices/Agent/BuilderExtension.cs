// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding hosted Python agent applications to the distributed application model.
/// </summary>
public static class PythonHostedAgentAppResourceBuilderExtensions
{
    /// <summary>
    /// Publish the Python agent as a hosted agent in Azure AI Foundry.
    ///
    /// This tries to avoid the infinite recursion issue from above by attaching the deployment
    /// of the compute object to another separate resource.
    /// </summary>
    /// <returns></returns>
    public static IResourceBuilder<T> PublishAsHostedAgent<T>(
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource>? project = null, Action<HostedAgentConfiguration>? configure = null)
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
            var projectResource = builder.ApplicationBuilder.Resources.OfType<AzureCognitiveServicesProjectResource>().FirstOrDefault()
                ?? throw new InvalidOperationException("AzureCognitiveServicesProjectResource must be present in the application model to publish hosted agents.");
            if (!builder.ApplicationBuilder.TryCreateResourceBuilder(projectResource.Name, out project))
            {
                throw new InvalidOperationException("Unable to create AzureCognitiveServicesProjectResource to publish hosted agent. Please specify the project explicitly.");
            }
        }
        var agentName = $"{resource.Name}-hosted-agent";
        if (builder.ApplicationBuilder.TryCreateResourceBuilder<AzureHostedAgentResource>(agentName, out var _))
        {
            // We already have a hosted agent for this resource
            // TODO: Should we update the agentBuilder configuration?
            return builder;
        }
        ContainerResource target;
        if (resource is ContainerResource containerResource)
        {
            target = containerResource;
        }
        else if (builder.ApplicationBuilder.TryCreateResourceBuilder<ContainerResource>(resource.Name, out var containerResourceBuilder))
        {
            target = containerResourceBuilder.Resource;
        }
        else
        {
            builder.PublishAsDockerFile();
            if (builder.ApplicationBuilder.TryCreateResourceBuilder<ContainerResource>(resource.Name, out var crb))
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
