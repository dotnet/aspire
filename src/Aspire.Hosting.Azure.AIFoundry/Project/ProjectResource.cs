// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Azure.Provisioning;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning.CognitiveServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Aspire.Hosting.Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// The Azure Cognitive Services project resource that can be used for Microsoft Foundry AI agents.
///
/// This also functions as an Aspire compute environment resource for deployment.
/// </summary>
public class AzureCognitiveServicesProjectResource :
    AzureResourceManagerAspireResourceWithParent<CognitiveServicesProject, AzureAIFoundryResource>,
    IResourceWithConnectionString,
    IAzureComputeEnvironmentResource,
    IAzureContainerRegistry
{
    /// <summary>
    /// Creates a new Azure Cognitive Services project resource.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configureInfrastructure"></param>
    /// <param name="parent"></param>
    public AzureCognitiveServicesProjectResource([ResourceName] string name, Action<AzureResourceInfrastructure> configureInfrastructure, AzureAIFoundryResource parent)
        : base(name, configureInfrastructure, parent)
    {
        // This is a bit long, but this sets up the necessary pipeline steps to
        // deploy hosted agents into this project. At a high level, that sequence is:
        // 1. (after project provisioning) Login to ACR (if needed)
        // 2. (for each compute resource) Build and push container image to ACR if needed
        // 3. For deployment target agent, deploy agent to project
        // 4. Mark deployment done
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();
            var loginStep = new PipelineStep
            {
                Name = $"login-to-acr-{name}",
                Action = context => AzureEnvironmentResourceHelpers.LoginToRegistryAsync(this, context),
                Tags = [LogInToAcrStepTag],
                Resource = this,
                DependsOnSteps = [AzureEnvironmentResource.ProvisionInfrastructureStepName],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy]
            };
            steps.Add(loginStep);

            var computeUrls = new PipelineStep
            {
                Name = $"compute-endpoints-{name}",
                Action = async (context) => {
                    var opts = context.Services.GetRequiredService<IOptions<AzureProvisionerOptions>>().Value;
                    var subscriptionId = opts.SubscriptionId;
                    var resourceGroupName = opts.ResourceGroup;
                    if (string.IsNullOrEmpty(subscriptionId) || string.IsNullOrEmpty(resourceGroupName))
                    {
                        return;
                    }
                    var encodedSubscriptionId = EncodeSubscriptionId(subscriptionId);
                    // Print dashboard URL
                    await context.ReportingStep.CompleteAsync(
                        $"https://ai.azure.com/nextgen/r/{encodedSubscriptionId},{resourceGroupName},,{Parent.Name},{Name}/home").ConfigureAwait(false);
                },
                Resource = this,
                DependsOnSteps = [AzureEnvironmentResource.ProvisionInfrastructureStepName],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy]
            };
            steps.Add(computeUrls);
            return steps;
        }));

        // Wire up inter-resource pipeline step dependencies
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var loginStep = context.GetSteps(this, LogInToAcrStepTag);

            foreach (var resource in context.Model.GetComputeResources())
            {
                var deploymentTarget = resource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;
                if (deploymentTarget is null)
                {
                    continue;
                }
                context.GetSteps(resource, WellKnownPipelineTags.PushContainerImage)
                    // Ensure image push steps happens after login to registry
                    .DependsOn(loginStep);
            }
        }));
    }

    /// <inheritdoc/>
    public override void SetName(CognitiveServicesProject provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }

    /// <inheritdoc/>
    public override CognitiveServicesProject FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesProject.FromExisting(bicepIdentifier);
    }

    /// <summary>
    /// Azure Resource ID output reference.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{ConnectionString}");

    /// <summary>
    /// Gets the "connectionString" output reference from the Azure Cognitive Services project resource.
    ///
    /// This will be used to instantiate the AI project clients.
    ///
    /// Will be of the format https:/{accountName}.services.ai.azure.com/api/projects/{projectName}?api-version={apiVersion}
    /// </summary>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the name of the environment variable to use for the connection string.
    /// </summary>
    public string ConnectionStringEnvironmentVariable { get; } = "AZURE_AI_PROJECT_ENDPOINT";

    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);
    internal BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);
    // User-assigned managed identity used for client access to container registry
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);
    internal BicepOutputReference ContainerRegistryClientId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_CLIENT_ID", this);

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId =>
        ReferenceExpression.Create($"{ContainerRegistryManagedIdentityId}");

    ReferenceExpression IContainerRegistry.Name =>
        ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint =>
        ReferenceExpression.Create($"{ContainerRegistryUrl}");

    /// <summary>
    /// Get the address for the particular agent's endpoint.
    /// </summary>
    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
    {
        var resource = endpointReference.Resource;
        return ReferenceExpression.Create($"{ConnectionString}/agents/{resource.Name}");
    }

    /// <summary>
    /// This is the encoding that the Foundry web portal uses in their URLs, for some reason
    /// </summary>
    internal static string EncodeSubscriptionId(string subscriptionId)
    {
        var guid = Guid.Parse(subscriptionId);
        var encoded = Base64Url.EncodeToString(guid.ToByteArray());
        return encoded.TrimEnd('=');
    }

    internal IContainerRegistry GetContainerRegistry()
    {
        if (this.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var annotation))
        {
            return annotation.Registry;
        }
        return this;
    }

    /// <summary>
    /// The tag name for the "login to ACR" publish/deploy pipeline step.
    /// </summary>
    public const string LogInToAcrStepTag = "login-to-acr-";

    /// <summary>
    /// Tries to get the application identity resource associated with this project via
    /// the <see cref="AppIdentityAnnotation"/>. This is the identity that will be
    /// used by the project to access other Azure resources such as the container registry.
    /// </summary>
    public bool TryGetAppIdentityResource([NotNullWhen(true)] out IAppIdentityResource? identity)
    {
        if (this.TryGetLastAnnotation<AppIdentityAnnotation>(out var identityAnnotation))
        {
            identity = identityAnnotation.IdentityResource;
            return true;
        }
        else
        {
            identity = null;
            return false;
        }
    }
}
