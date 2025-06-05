// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AIFoundry;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure AI Foundry resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Configures the underlying Azure resource using Azure.Provisioning.</param>
public class AzureAIFoundryResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    private readonly List<AzureAIFoundryDeploymentResource> _deployments = [];

    /// <summary>
    /// Gets the "connectionString" output reference from the Azure AI Services resource.
    /// </summary>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    /// <summary>
    /// Gets the list of deployment resources associated with the Azure AI Foundry.
    /// </summary>
    public IReadOnlyList<AzureAIFoundryDeploymentResource> Deployments => _deployments;

    internal void AddDeployment(AzureAIFoundryDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        _deployments.Add(deployment);
    }

    internal ReferenceExpression GetConnectionString(string deploymentName) =>
        ReferenceExpression.Create($"{ConnectionString};DeploymentId={deploymentName}");

    internal AzureAIFoundryLocalResource? InnerResource { get; private set; }

    internal void SetInnerResource(AzureAIFoundryLocalResource innerResource)
    {
        // Copy the annotations to the inner resource before making it the inner resource
        foreach (var annotation in Annotations)
        {
            innerResource.Annotations.Add(annotation);
        }

        InnerResource = innerResource;
    }
}
