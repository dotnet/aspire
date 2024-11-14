// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using static Azure.Provisioning.Expressions.BicepFunction;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure OpenAI resources to the application model.
/// </summary>
public static class AzureOpenAIExtensions
{
    internal const string DefaultConfigSectionName = "Aspire:Azure:AI:OpenAI";

    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var cogServicesAccount = new CognitiveServicesAccount(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Kind = "OpenAI",
                Sku = new CognitiveServicesSku()
                {
                    Name = "S0"
                },
                Properties = new CognitiveServicesAccountProperties()
                {
                    CustomSubDomainName = ToLower(Take(Concat(infrastructure.AspireResource.Name, GetUniqueString(GetResourceGroup().Id)), 24)),
                    PublicNetworkAccess = ServiceAccountPublicNetworkAccess.Enabled,
                    // Disable local auth for AOAI since managed identity is used
                    DisableLocalAuth = true
                },
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(cogServicesAccount);

            infrastructure.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                 Value = Interpolate($"Endpoint={cogServicesAccount.Properties.Endpoint}")
            });

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(cogServicesAccount.CreateRoleAssignment(CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor, principalTypeParameter, principalIdParameter));

            var resource = (AzureOpenAIResource)infrastructure.AspireResource;

            CognitiveServicesAccountDeployment? dependency = null;

            var cdkDeployments = new List<CognitiveServicesAccountDeployment>();
            foreach (var deployment in resource.Deployments)
            {
                var cdkDeployment = new CognitiveServicesAccountDeployment(Infrastructure.NormalizeBicepIdentifier(deployment.Name))
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
                infrastructure.Add(cdkDeployment);
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

        var resource = new AzureOpenAIResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment to the <see cref="AzureOpenAIResource"/> resource. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure OpenAI resource builder.</param>
    /// <param name="deployment">The deployment to add.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> builder, AzureOpenAIDeployment deployment)
    {
        builder.Resource.AddDeployment(deployment);

        return builder;
    }

    /// <summary>
    /// Injects the environment variables from the source <see cref="AzureOpenAIResource" /> into the destination resource, using the source resource's name as the connection string name (if not overridden).
    /// The format of the connection environment variable will be "ConnectionStrings__{sourceResourceName}={connectionString}".
    /// Each deployment will be injected using the format "Aspire__Azure__AI__OpenAI__{sourceResourceName}__Models__{deploymentName}={modelName}".
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where connection string will be injected.</param>
    /// <param name="source">The resource from which to extract the connection string.</param>
    /// <param name="resourceName">An override of the source resource's name for the connection string. The resulting connection string will be "ConnectionStrings__connectionName" if this is not null.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<AzureOpenAIResource> source, string? resourceName = null)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        var resource = source.Resource;
        resourceName ??= resource.Name;

        builder.WithReference((IResourceBuilder<IResourceWithConnectionString>)source, resourceName);

        return builder.WithEnvironment(context =>
        {
            foreach (var deployment in resource.Deployments)
            {
                var variableName = $"ASPIRE__AZURE__AI__OPENAI__{resourceName}__MODELS__{deployment.Name}";
                context.EnvironmentVariables[variableName] = deployment.ModelName;
            }
        });
    }
}
