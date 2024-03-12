// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.CognitiveServices;
using Azure.ResourceManager.CognitiveServices.Models;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure OpenAI resources to the application model.
/// </summary>
public static class AzureOpenAIExtensions
{
    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureOpenAIResource(name);
        return builder.AddResource(resource)
                      .WithParameter("name", resource.CreateBicepResourceName())
                      .WithParameter("deployments", () => GetDeploymentsAsJson(resource))
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIConstructResource> AddAzureOpenAIConstruct(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureOpenAIConstructResource>, ResourceModuleConstruct, CognitiveServicesAccount, IEnumerable<CognitiveServicesAccountDeployment>>? configureResource = null)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cogServicesAccount = new CognitiveServicesAccount(construct, "OpenAI", name: name);
            cogServicesAccount.AssignProperty(x => x.Properties.CustomSubDomainName, $"toLower(take(concat('{name}', uniqueString(resourceGroup().id)), 24))");
            cogServicesAccount.AssignProperty(x => x.Properties.PublicNetworkAccess, "'Enabled'");
            cogServicesAccount.AddOutput("connectionString", """'Endpoint=${{{0}}}'""", x => x.Properties.Endpoint);

            var roleAssignment = cogServicesAccount.AssignRole(RoleDefinition.CognitiveServicesOpenAIContributor);
            roleAssignment.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            roleAssignment.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            var resource = (AzureOpenAIConstructResource)construct.Resource;

            var cdkDeployments = new List<CognitiveServicesAccountDeployment>();
            foreach (var deployment in resource.Deployments)
            {
                var model = new CognitiveServicesAccountDeploymentModel();
                model.Name = deployment.ModelName;
                model.Version = deployment.ModelVersion;
                model.Format = "OpenAI";

                var cdkDeployment = new CognitiveServicesAccountDeployment(construct, model, parent: cogServicesAccount, name: deployment.Name);
                cdkDeployment.AssignProperty(x => x.Sku.Name, $"'{deployment.SkuName}'");
                cdkDeployment.AssignProperty(x => x.Sku.Capacity, $"{deployment.SkuCapacity}");
            }

            if (configureResource != null)
            {
                var resourceBuilder = builder.CreateResourceBuilder(resource);
                configureResource(resourceBuilder, construct, cogServicesAccount, cdkDeployments);
            }
        };

        var resource = new AzureOpenAIConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment resource to the application model. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure OpenAI resource builder.</param>
    /// <param name="deployment">The deployment to add.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> WithDeployment(this IResourceBuilder<AzureOpenAIResource> builder, AzureOpenAIDeployment deployment)
    {
        builder.Resource.AddDeployment(deployment);
        return builder;
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment resource to the application model. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure OpenAI resource builder.</param>
    /// <param name="deployment">The deployment to add.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIConstructResource> AddDeployment(this IResourceBuilder<AzureOpenAIConstructResource> builder, AzureOpenAIDeployment deployment)
    {
        builder.Resource.AddDeployment(deployment);
        return builder;
    }

    internal static JsonArray GetDeploymentsAsJson(AzureOpenAIResource resource)
    {
        return new JsonArray(
            resource.Deployments.Select(deployment => new JsonObject
            {
                ["name"] = deployment.Name,
                ["sku"] = new JsonObject
                {
                    ["name"] = deployment.SkuName,
                    ["capacity"] = deployment.SkuCapacity
                },
                ["model"] = new JsonObject
                {
                    ["format"] = "OpenAI",
                    ["name"] = deployment.ModelName,
                    ["version"] = deployment.ModelVersion
                }
            }).ToArray());
    }
}
