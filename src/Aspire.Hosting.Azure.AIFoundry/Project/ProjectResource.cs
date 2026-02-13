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
    AzureProvisionableAspireResourceWithParent<CognitiveServicesProject, AzureAIFoundryResource>,
    IResourceWithConnectionString,
    IAzureComputeEnvironmentResource,
    IContainerRegistry
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

            var computeUrls = new PipelineStep
            {
                Name = $"compute-endpoints-{name}",
                Action = async (context) =>
                {
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
    /// Gets the .NET-style connection string for the project API endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Endpoint={Endpoint}");

    /// <summary>
    /// Gets the Foundry project endpoint as a ReferenceExpression that can be used in environment variables or connection strings.
    ///
    /// This will be used to instantiate the AI project clients.
    ///
    /// Will be of the format https:/{accountName}.services.ai.azure.com/api/projects/{projectName}?api-version={apiVersion}
    /// </summary>
    public ReferenceExpression UriExpression => ReferenceExpression.Create($"{Endpoint}");

    /// <summary>
    /// Gets the "endpoint" output reference from the Azure Cognitive Services project resource.
    ///
    /// This will be used to instantiate the AI project clients.
    ///
    /// Will be of the format https:/{accountName}.services.ai.azure.com/api/projects/{projectName}?api-version={apiVersion}
    /// </summary>
    public BicepOutputReference Endpoint => new("endpoint", this);

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties()
    {
        yield return new("Uri", UriExpression);
        // Without this, `.WithReference(project)` causes an error like "waiting for connection string for resource {projectName}"
        yield return new("ConnectionString", ConnectionStringExpression);
    }

    /// <summary>
    /// Gets the managed identity principal ID, whether system-assigned or user-assigned.
    /// </summary>
    public BicepOutputReference PrincipalId => new("principalId", this);

    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);
    internal BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);
    // Mnaged identity used for client access to container registry
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    ReferenceExpression IContainerRegistry.Name => ContainerRegistry?.Name ?? ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint => ContainerRegistry?.Endpoint ?? ReferenceExpression.Create($"{ContainerRegistryUrl}");

    /// <summary>
    /// The Application Insights resource associated with this project, if any.
    ///
    /// This will be used as the destination for server-side telemetry from hosted
    /// agents and optionally for client-side telemetry if the application decides
    /// to use it.
    /// </summary>
    public AzureApplicationInsightsResource? AppInsights { get; set; }

    internal BicepOutputReference AppInsightsConnectionString => new("APPLICATION_INSIGHTS_CONNECTION_STRING", this);

    /// <summary>
    /// Connection to the user-assigned key vault, if any.
    /// </summary>
    public AzureCognitiveServicesProjectConnectionResource? KeyVaultConn { get; set; }

    /// <summary>
    /// The capability host resources associated with this project, if any
    /// </summary>
    public CapabilityHostConfiguration? CapabilityHostConfiguration { get; set; }

    /// <summary>
    /// The container registry associated with this project, if any.
    /// </summary>
    public IContainerRegistry? ContainerRegistry
    {
        get
        {
            if (this.TryGetLastAnnotation<ContainerRegistryReferenceAnnotation>(out var annotation))
            {
                return annotation.Registry;
            }
            return DefaultContainerRegistry;
        }
    }

    internal AzureContainerRegistryResource? DefaultContainerRegistry { get; set; }

    /// <summary>
    /// Get the address for the particular agent's endpoint.
    /// </summary>
    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
    {
        var resource = endpointReference.Resource;
        return ReferenceExpression.Create($"{Endpoint}/agents/{resource.Name}");
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

/// <summary>
/// Configuration for an Azure Cognitive Services capability host.
///
/// This is a separate class to ensure that we set all storage resources or none.
/// </summary>
public class CapabilityHostConfiguration(string name, AzureCosmosDBResource cosmosDB, AzureStorageResource storage, AzureSearchResource search)
{
    /// <summary>
    /// The name of the capability host.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// The kind of capability host.
    /// </summary>
    public CapabilityHostKind Kind { get; set; } = CapabilityHostKind.Agents;

    /// <summary>
    /// The Cosmos DB resource to use for metadata and conversation state storage.
    /// </summary>
    public AzureCosmosDBResource CosmosDB { get; set; } = cosmosDB;

    /// <summary>
    /// The Storage resource to use for file storage.
    /// </summary>
    public AzureStorageResource Storage { get; set; } = storage;

    /// <summary>
    /// The Azure Search resource to use for vector search capabilities.
    /// </summary>
    public AzureSearchResource Search { get; set; } = search;

    /// <summary>
    /// An OpenAI-type Foundry account to use for AI model calls, if any.
    /// </summary>
    public AzureAIFoundryResource? AzureOpenAI { get; set; }
}
