// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Core;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding hosted agent applications to the distributed application model.
/// </summary>
public static class HostedAgentResourceBuilderExtensions
{
    private static readonly AzureLocation[] s_supportedHostedAgentRegions =
    [
        AzureLocation.BrazilSouth,
        AzureLocation.CanadaEast,
        AzureLocation.EastUS,
        AzureLocation.FranceCentral,
        AzureLocation.GermanyWestCentral,
        AzureLocation.ItalyNorth,
        AzureLocation.NorthCentralUS,
        AzureLocation.SouthAfricaNorth,
        AzureLocation.SouthCentralUS,
        AzureLocation.SouthIndia,
        AzureLocation.SpainCentral,
        AzureLocation.SwedenCentral,
        AzureLocation.CanadaCentral,
        AzureLocation.KoreaCentral,
        AzureLocation.SoutheastAsia,
        AzureLocation.AustraliaEast,
        AzureLocation.EastUS2,
        AzureLocation.JapanEast,
        AzureLocation.UAENorth,
        AzureLocation.UKSouth,
        AzureLocation.WestUS,
        AzureLocation.WestUS3,
        AzureLocation.NorwayEast,
        AzureLocation.PolandCentral,
        AzureLocation.SwitzerlandNorth
    ];

    private static readonly HashSet<string> s_supportedHostedAgentRegionKeys = s_supportedHostedAgentRegions
        .Select(static region => region.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// In both run and publish modes, build, deploy, and run the containerized agent as a hosted agent in Azure AI Foundry.
    /// </summary>
    public static IResourceBuilder<T> AsHostedAgent<T>(
        this IResourceBuilder<T> builder, Action<HostedAgentConfiguration>? configure = null)
        where T : ExecutableResource
    {
        return builder.AsHostedAgent(project: null, configure: configure).PublishAsHostedAgent(project: null, configure: configure);
    }

    /// <summary>
    /// In both run and publish modes, build, deploy, and run the containerized agent as a hosted agent in Azure AI Foundry.
    /// </summary>
    public static IResourceBuilder<T> AsHostedAgent<T>(
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource>? project = null, Action<HostedAgentConfiguration>? configure = null)
        where T : ExecutableResource
    {
        return builder.RunAsHostedAgent(project: project, configure: configure).PublishAsHostedAgent(project: project, configure: configure);
    }

    /// <summary>
    /// In run mode, build, deploy, and run the containerized agent as a hosted agent in Azure AI Foundry.
    /// </summary>
    public static IResourceBuilder<T> RunAsHostedAgent<T>(
        this IResourceBuilder<T> builder, Action<HostedAgentConfiguration> configure)
        where T : ExecutableResource
    {
        return builder.RunAsHostedAgent(project: null, configure: configure);
    }

    /// <summary>
    /// In run mode, build, deploy, and run the containerized agent as a hosted agent in Azure AI Foundry.
    /// </summary>
    public static IResourceBuilder<T> RunAsHostedAgent<T>(
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource>? project = null, Action<HostedAgentConfiguration>? configure = null)
        where T : ExecutableResource
    {
        // TODO: implement this
        throw new NotImplementedException("RunAsHostedAgent is not yet implemented.");
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
        this IResourceBuilder<T> builder, IResourceBuilder<AzureCognitiveServicesProjectResource>? project = null, Action<HostedAgentConfiguration>? configure = null)
        where T : ExecutableResource
    {
        /*
         * Much of the logic here is similar to ExecutableResourceBuilderExtensions.PublishAsDockerFile().
         *
         * That is, in Publish mode, we swap the original resource with a hosted agent resource.
         */
        ArgumentNullException.ThrowIfNull(builder);

        var resource = builder.Resource;
        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        ValidateHostedAgentRegion(builder.ApplicationBuilder.Configuration);

        AzureCognitiveServicesProjectResource? projResource;;
        if (project is not null)
        {
            projResource = project.Resource;
        }
        else
        {
            projResource = builder.ApplicationBuilder.Resources.OfType<AzureCognitiveServicesProjectResource>().FirstOrDefault();
            if (projResource is null)
            {
                project = builder.ApplicationBuilder.AddAzureAIFoundryProject($"{resource.Name}-proj");
                projResource = project.Resource;
            }
            else
            {
                project = builder.ApplicationBuilder.CreateResourceBuilder(projResource);
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
            // Ensure we have a container resource to deploy
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
            ComputeEnvironment = projResource,
            ContainerRegistry = projResource.ContainerRegistry
        });

        builder.ApplicationBuilder.AddResource(agent)
            .WithReferenceRelationship(target)
            .WithReference(project);

        return builder;
    }

    private static void ValidateHostedAgentRegion(IConfiguration configuration)
    {
        var location = configuration["Azure:Location"];
        if (string.IsNullOrWhiteSpace(location))
        {
            return;
        }

        var azureLocation = new AzureLocation(location);
        if (!s_supportedHostedAgentRegionKeys.Contains(azureLocation.Name))
        {
            var supportedRegions = string.Join(", ", s_supportedHostedAgentRegions.Select(r => r.DisplayName));
            throw new InvalidOperationException($"Azure location '{location}' is not supported for hosted agents. Supported regions: {supportedRegions}.");
        }
    }

    /// <summary>
    /// Publish a simple prompt agent in Azure AI Foundry.
    ///
    /// If a project resource is not provided, the method will attempt to find an existing
    /// Azure Cognitive Services Project resource in the application model.
    /// </summary>
    public static IResourceBuilder<AzurePromptAgentResource> AddAndPublishPromptAgent(
        this IResourceBuilder<AzureCognitiveServicesProjectResource> project, IResourceBuilder<AzureAIFoundryDeploymentResource> model, [ResourceName] string name, string? instructions)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(model);
        var agent = new AzurePromptAgentResource(name, model.Resource.DeploymentName, instructions);
        return project.ApplicationBuilder.AddResource(agent)
            .WithReferenceRelationship(project)
            .WithArgs([
                // TODO: actually execute the prompt agent locally
                "-c",
                "--project", project.Resource.Endpoint,
                "--model", model.Resource.DeploymentName,
                "--instructions", instructions ?? string.Empty,
            ]);
    }
}
