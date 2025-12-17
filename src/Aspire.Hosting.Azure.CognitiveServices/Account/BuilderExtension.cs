// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.CognitiveServices;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;
using static Azure.Provisioning.Expressions.BicepFunction;
using Microsoft.Extensions.DependencyInjection;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Resources;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for adding Azure Cognitive Services account resources to the distributed application model.
/// </summary>
public static class AzureCognitiveServicesAccountBuilderExtensions
{
    /// <summary>
    /// Adds an Azure Cognitive Services account resource to the distributed application model.
    /// </summary>
    /// <remarks>This method configures the resource with default settings suitable for most Azure AI projects scenarios.</remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the Azure Cognitive Services account resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCognitiveServicesAccountResource> AddAzureCognitiveServicesAccount(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Ensure Azure provisioning runs as a step for the builder
        builder.AddAzureProvisioning();

        builder.Services.Configure<AzureProvisioningOptions>(options => options.SupportsTargetedRoleAssignments = true);

        void configureInfrastructure(AzureResourceInfrastructure infrastructure)
        {
            var aspireResource = (AzureCognitiveServicesAccountResource)infrastructure.AspireResource;
            var cogServicesAccount = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(
                infrastructure,
                (identifier, resourceName) =>
                {
                    var resource = aspireResource.FromExisting(identifier);
                    resource.Name = resourceName;
                    return resource;
                },
                infra =>
                {
                    var account = new CognitiveServicesAccount(infra.AspireResource.GetBicepIdentifier())
                    {
                        Name = name,
                        Kind = "AIServices",
                        Sku = new CognitiveServicesSku
                        {
                            Name = "S0"
                        },
                        Identity = new ManagedServiceIdentity()
                        {
                            ManagedServiceIdentityType = ManagedServiceIdentityType.SystemAssigned
                        },
                        Properties = new CognitiveServicesAccountProperties
                        {
                            CustomSubDomainName = ToLower(Take(Concat(infra.AspireResource.Name, GetUniqueString(GetResourceGroup().Id)), 24)),
                            PublicNetworkAccess = ServiceAccountPublicNetworkAccess.Enabled,
                            DisableLocalAuth = true,
                            AllowProjectManagement = true
                        },
                        Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
                    };

                    return account;
                });
            infrastructure.Add(new ProvisioningOutput("connectionString", typeof(string))
            {
                Value = (BicepValue<string>)new IndexExpression((BicepExpression)cogServicesAccount.Properties.Endpoints!, "AI Foundry API")
            });
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = cogServicesAccount.Name });
            infrastructure.Add(new ProvisioningOutput("id", typeof(string)) { Value = cogServicesAccount.Id });
        }

        var resource = new AzureCognitiveServicesAccountResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(CognitiveServicesBuiltInRole.GetBuiltInRoleName, CognitiveServicesBuiltInRole.CognitiveServicesContributor)
            .WithDefaultRoleAssignments(CognitiveServicesBuiltInRole.GetBuiltInRoleName, CognitiveServicesBuiltInRole.CognitiveServicesUser);
    }
}
