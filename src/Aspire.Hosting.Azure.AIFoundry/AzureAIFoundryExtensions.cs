// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.AIFoundry;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;
using static Azure.Provisioning.Expressions.BicepFunction;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AI Services resources to the application model.
/// </summary>
public static class AzureAIFoundryExtensions
{
    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryResource> AddAzureAIFoundry(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var cogServicesAccount = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = CognitiveServicesAccount.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new CognitiveServicesAccount(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    Name = Take(Interpolate($"{infrastructure.AspireResource.GetBicepIdentifier()}{GetUniqueString(GetResourceGroup().Id)}"), 64),
                    Kind = "AIServices",
                    Sku = new CognitiveServicesSku()
                    {
                        Name = "S0"
                    },
                    Properties = new CognitiveServicesAccountProperties()
                    {
                        CustomSubDomainName = ToLower(Take(Concat(infrastructure.AspireResource.Name, GetUniqueString(GetResourceGroup().Id)), 24)),
                        PublicNetworkAccess = ServiceAccountPublicNetworkAccess.Enabled,
                        DisableLocalAuth = true,
                        // TODO: May need to enable project management (might require a custom CDK resource to set the property, and custom API version)
                        // AllowProjectManagement = true,
                    },
                    Identity = new ManagedServiceIdentity()
                    {
                        ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
                    },
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            var inferenceEndpoint = (BicepValue<string>)new IndexExpression(
                (BicepExpression)cogServicesAccount.Properties.Endpoints!,
                "AI Foundry API");
            infrastructure.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = Interpolate($"Endpoint={inferenceEndpoint}")
            });

            var resource = (AzureAIFoundryResource)infrastructure.AspireResource;

            CognitiveServicesAccountDeployment? dependency = null;
            foreach (var deployment in resource.Deployments)
            {
                var cdkDeployment = new CognitiveServicesAccountDeployment(Infrastructure.NormalizeBicepIdentifier(deployment.Name))
                {
                    Name = deployment.DeploymentName,
                    Parent = cogServicesAccount,
                    Properties = new CognitiveServicesAccountDeploymentProperties()
                    {
                        Model = new CognitiveServicesAccountDeploymentModel()
                        {
                            Name = deployment.ModelName,
                            Version = deployment.ModelVersion,
                            Format = deployment.Format
                        }
                    },
                    Sku = new CognitiveServicesSku()
                    {
                        Name = deployment.SkuName,
                        Capacity = deployment.SkuCapacity
                    }
                };
                infrastructure.Add(cdkDeployment);

                // Subsequent deployments need an explicit dependency on the previous one
                // to ensure they are not created in parallel. This is equivalent to @batchSize(1)
                // which can't be defined with the CDK

                // TODO: Check if this is really necessary. If so should it be removed once deployed or will deployments
                // suppression be an issue?

                if (dependency != null)
                {
                    cdkDeployment.DependsOn.Add(dependency);
                }

                dependency = cdkDeployment;

            }
        };

        var resource = new AzureAIFoundryResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds and returns an Azure AI Services Deployment resource to the application model.
    /// </summary>
    /// <param name="builder">The Azure AI Services resource builder.</param>
    /// <param name="name">The name of the Azure AI Services Deployment resource.</param>
    /// <param name="modelName">The name of the model to deploy.</param>
    /// <param name="modelVersion">The version of the model to deploy.</param>
    /// <param name="format">The format of the model to deploy.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> AddDeployment(this IResourceBuilder<AzureAIFoundryResource> builder, [ResourceName] string name, string modelName, string modelVersion, string format)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(modelName);
        ArgumentException.ThrowIfNullOrEmpty(modelVersion);
        ArgumentException.ThrowIfNullOrEmpty(format);

        var deployment = new AzureAIFoundryDeploymentResource(name, modelName, modelVersion, format, builder.Resource);

        builder.Resource.AddDeployment(deployment);

        var resourceBuilder = builder.ApplicationBuilder
                .CreateResourceBuilder(deployment);

        if (builder.Resource.IsLocal)
        {
            resourceBuilder.ConfigureLocalDeployment(deployment);
        }

        return resourceBuilder;
    }

    /// <summary>
    /// Allows setting the properties of an Azure AI Services Deployment resource.
    /// </summary>
    /// <param name="builder">The Azure AI Services Deployment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="AzureAIFoundryDeploymentResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAIFoundryDeploymentResource> WithProperties(this IResourceBuilder<AzureAIFoundryDeploymentResource> builder, Action<AzureAIFoundryDeploymentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }
}
