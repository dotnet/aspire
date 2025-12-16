// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services account resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesDeploymentsBuilderExtensions
{
    /// <summary>
    /// Adds an AI model deployment to the Azure Cognitive Services account.
    /// </summary>
    /// <param name="builder">The Azure Cognitive Services account builder.</param>
    /// <param name="name">The name of the deployment resource.</param>
    /// <param name="modelFormat">The model format a.k.a. provider (e.g., "OpenAI").</param>
    /// <param name="modelName">The model name.</param>
    /// <param name="modelVersion">The model version.</param>
    /// <param name="configure">An optional action to further configure the deployment.</param>
    /// <returns>An <see cref="IResourceBuilder{T}"/> for the deployment.</returns>
    /// <remarks>
    /// To see a list of valid models, run
    /// `az cognitiveservices account list-models --name [account-name] --resource-group [resource-group-name]`.
    /// </remarks>
    public static IResourceBuilder<AzureCognitiveServicesAccountDeploymentResource> AddDeployment(
        this IResourceBuilder<AzureCognitiveServicesAccountResource> builder,
        string name,
        string modelFormat,
        string modelName,
        string? modelVersion = null,
        Action<CognitiveServicesAccountDeployment>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(modelFormat);
        ArgumentException.ThrowIfNullOrEmpty(modelName);

        var parent = builder.Resource;

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var aspireResource = (AzureCognitiveServicesAccountDeploymentResource)infrastructure.AspireResource;
            var account = builder.Resource.AddAsExistingResource(infrastructure);
            var cogServicesDeployment = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, resourceName) =>
                {
                    var resource = aspireResource.FromExisting(identifier);
                    resource.Name = resourceName;
                    resource.Parent = account;
                    return resource;
                },
                infra =>
                {
                    var model = new CognitiveServicesAccountDeploymentModel
                    {
                        Format = modelFormat,
                        Name = modelName,
                    };
                    if (!string.IsNullOrEmpty(modelVersion))
                    {
                        model.Version = modelVersion;
                    }
                    var resource = new CognitiveServicesAccountDeployment(infra.AspireResource.GetBicepIdentifier())
                    {
                        Parent = account,
                        Name = name,
                        Properties = new CognitiveServicesAccountDeploymentProperties
                        {
                            Model = model
                        },
                        Sku = new CognitiveServicesSku
                        {
                            Name = "GlobalStandard",
                            Capacity = 1
                        }
                    };
                    configure?.Invoke(resource);
                    return resource;
                });
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = cogServicesDeployment.Name });
        }
        AzureCognitiveServicesAccountDeploymentResource deploymentResource = new(name, configureInfrastructure, parent);
        return builder.ApplicationBuilder.AddResource(deploymentResource);
    }

    /// <summary>
    /// Adds an AI model deployment to the Azure Cognitive Services account with default model format and name
    /// "OpenAI" and "gpt-4.1-mini".
    /// </summary>
    public static IResourceBuilder<AzureCognitiveServicesAccountDeploymentResource> AddDeployment(this IResourceBuilder<AzureCognitiveServicesAccountResource> builder, [ResourceName] string name)
    {
        return builder.AddDeployment(name, "OpenAI", "gpt-4.1-mini", "2025-04-14");
    }

    /// <summary>
    /// Adds a reference to the Azure Cognitive Services account deployment resource.
    /// </summary>
    public static IResourceBuilder<T> WithReference<T>(this IResourceBuilder<T> builder,
        IResourceBuilder<AzureCognitiveServicesAccountDeploymentResource> deployment)
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(deployment);
        return builder.WithEnvironment("AZURE_AI_DEPLOYMENT_NAME", $"{deployment.Resource.Name}");
    }
}
