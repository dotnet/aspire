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
    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// By default references to the Azure OpenAI resource will be assigned the following roles:
    /// 
    /// - <see cref="CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureOpenAIResource}, CognitiveServicesBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

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
                });

            infrastructure.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = Interpolate($"Endpoint={cogServicesAccount.Properties.Endpoint}")
            });

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = cogServicesAccount.Name });

            var resource = (AzureOpenAIResource)infrastructure.AspireResource;

            CognitiveServicesAccountDeployment? dependency = null;

#pragma warning disable CS0618 // Type or member is obsolete
            foreach (var deployment in resource.Deployments)
            {
                dependency = CreateDeployment(infrastructure, cogServicesAccount, dependency, deployment);
            }

            foreach (var deployment in resource.DeploymentResources)
            {
                dependency = CreateDeployment(
                    infrastructure,
                    cogServicesAccount,
                    dependency,
                    new AzureOpenAIDeployment(
                        deployment.DeploymentName,
                        deployment.ModelName,
                        deployment.ModelVersion,
                        deployment.SkuName,
                        deployment.SkuCapacity));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        };

        var resource = new AzureOpenAIResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(CognitiveServicesBuiltInRole.GetBuiltInRoleName,
                CognitiveServicesBuiltInRole.CognitiveServicesOpenAIContributor);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private static CognitiveServicesAccountDeployment CreateDeployment(AzureResourceInfrastructure infrastructure, CognitiveServicesAccount cogServicesAccount, CognitiveServicesAccountDeployment? dependency, AzureOpenAIDeployment deployment)
#pragma warning restore CS0618 // Type or member is obsolete
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

        // Subsequent deployments need an explicit dependency on the previous one
        // to ensure they are not created in parallel. This is equivalent to @batchSize(1)
        // which can't be defined with the CDK

        if (dependency != null)
        {
            cdkDeployment.DependsOn.Add(dependency);
        }

        return cdkDeployment;
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment to the <see cref="AzureOpenAIResource"/> resource. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure OpenAI resource builder.</param>
    /// <param name="deployment">The deployment to add.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete("AddDeployment taking an AzureOpenAIDeployment is deprecated. Please the AddDeployment overload that returns an AzureOpenAIDeploymentResource instead.")]
    public static IResourceBuilder<AzureOpenAIResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> builder, AzureOpenAIDeployment deployment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(deployment);

        builder.Resource.AddDeployment(deployment);

        return builder;
    }

    /// <summary>
    /// Adds and returns an Azure OpenAI Deployment resource to the <see cref="AzureOpenAIResource"/> resource.
    /// </summary>
    /// <param name="builder">The Azure OpenAI resource builder.</param>
    /// <param name="name">The name of the Azure OpenAI Deployment resource.</param>
    /// <param name="modelName">The name of the model to deploy.</param>
    /// <param name="modelVersion">The version of the model to deploy.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIDeploymentResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> builder, [ResourceName] string name, string modelName, string modelVersion)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(modelName);
        ArgumentException.ThrowIfNullOrEmpty(modelVersion);

        var deployment = new AzureOpenAIDeploymentResource(name, modelName, modelVersion, builder.Resource);
        builder.Resource.AddDeployment(deployment);

        return builder.ApplicationBuilder.AddResource(deployment);
    }

    /// <summary>
    /// Allows setting the properties of an Azure OpenAI Deployment resource.
    /// </summary>
    /// <param name="builder">The Azure OpenAI Deployment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="AzureOpenAIDeploymentResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIDeploymentResource> WithProperties(this IResourceBuilder<AzureOpenAIDeploymentResource> builder, Action<AzureOpenAIDeploymentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure OpenAI resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure OpenAI resource.</param>
    /// <param name="roles">The built-in Cognitive Services roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <example>
    /// Assigns the CognitiveServicesOpenAIUser role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var openai = builder.AddAzureOpenAI("openai");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(openai, CognitiveServicesBuiltInRole.CognitiveServicesOpenAIUser)
    ///   .WithReference(openai);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureOpenAIResource> target,
        params CognitiveServicesBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, CognitiveServicesBuiltInRole.GetBuiltInRoleName, roles);
    }
}
