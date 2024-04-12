// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
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
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureOpenAI(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.CognitiveServices.CognitiveServicesAccount"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureOpenAIResource>, ResourceModuleConstruct, CognitiveServicesAccount, IEnumerable<CognitiveServicesAccountDeployment>>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cogServicesAccount = new CognitiveServicesAccount(construct, "OpenAI", name: name);
            cogServicesAccount.AssignProperty(x => x.Properties.CustomSubDomainName, $"toLower(take(concat('{name}', uniqueString(resourceGroup().id)), 24))");
            cogServicesAccount.AssignProperty(x => x.Properties.PublicNetworkAccess, "'Enabled'");
            cogServicesAccount.AddOutput("connectionString", """'Endpoint=${{{0}}}'""", x => x.Properties.Endpoint);

            var roleAssignment = cogServicesAccount.AssignRole(RoleDefinition.CognitiveServicesOpenAIContributor);
            roleAssignment.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            roleAssignment.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            var resource = (AzureOpenAIResource)construct.Resource;

            CognitiveServicesAccountDeployment? dependency = null;

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
                cdkDeployments.Add(cdkDeployment);

                // Subsequent deployments need an explicit dependency on the previous one
                // to ensure they are not created in parallel. This is equivalent to @batchSize(1)
                // which can't be defined with the CDK

                if (dependency != null)
                {
                    cdkDeployment.AddDependency(dependency);
                }

                dependency = cdkDeployment;
            }

            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, cogServicesAccount, cdkDeployments);
        };

        var resource = new AzureOpenAIResource(name, configureConstruct);
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
    public static IResourceBuilder<AzureOpenAIResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> builder, AzureOpenAIDeployment deployment)
    {
        builder.Resource.AddDeployment(deployment);
        return builder;
    }
}
