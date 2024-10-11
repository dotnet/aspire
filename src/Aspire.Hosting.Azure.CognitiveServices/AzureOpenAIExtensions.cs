// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Expressions;
using static Azure.Provisioning.Expressions.BicepFunction;

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
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cogServicesAccount = new CognitiveServicesAccount(construct.Resource.GetBicepIdentifier())
            {
                Kind = "OpenAI",
                Sku = new CognitiveServicesSku()
                {
                    Name = "S0"
                },
                Properties = new CognitiveServicesAccountProperties()
                {
                    CustomSubDomainName = ToLower(Take(Concat(construct.Resource.Name, GetUniqueString(GetResourceGroup().Id)), 24)),
                    PublicNetworkAccess = ServiceAccountPublicNetworkAccess.Enabled,
                    // Disable local auth for AOAI since managed identity is used
                    DisableLocalAuth = true
                },
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(cogServicesAccount);

            construct.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = new InterpolatedString(
                        "Endpoint={0}",
                        [
                            new MemberExpression(
                                new MemberExpression(
                                    new IdentifierExpression(cogServicesAccount.IdentifierName),
                                    "properties"),
                                "endpoint")
                        ])
                // TODO This should be
                // Value = BicepFunction.Interpolate($"Endpoint={cogServicesAccount.Endpoint}")
            });

            construct.Add(cogServicesAccount.CreateRoleAssignment(CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

            var resource = (AzureOpenAIResource)construct.Resource;

            CognitiveServicesAccountDeployment? dependency = null;

            var cdkDeployments = new List<CognitiveServicesAccountDeployment>();
            foreach (var deployment in resource.Deployments)
            {
                var cdkDeployment = new CognitiveServicesAccountDeployment(Infrastructure.NormalizeIdentifierName(deployment.Name))
                {
                    Name = deployment.Name,
                    Parent = cogServicesAccount,
                    Properties = new CognitiveServicesAccountDeploymentProperties()
                    {
                        Model = new CognitiveServicesAccountDeploymentModel()
                        {
                            Name = deployment.ModelName,
                            Version = deployment.ModelVersion,
                            Format = "OpenAI"
                        }
                    },
                    Sku = new CognitiveServicesSku()
                    {
                        Name = deployment.SkuName,
                        Capacity = deployment.SkuCapacity
                    }
                };
                construct.Add(cdkDeployment);
                cdkDeployments.Add(cdkDeployment);

                // Subsequent deployments need an explicit dependency on the previous one
                // to ensure they are not created in parallel. This is equivalent to @batchSize(1)
                // which can't be defined with the CDK

                if (dependency != null)
                {
                    cdkDeployment.DependsOn.Add(dependency);
                }

                dependency = cdkDeployment;
            }
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
