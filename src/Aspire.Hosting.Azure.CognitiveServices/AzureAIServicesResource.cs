// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure AI Services resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Configures the underlying Azure resource using Azure.Provisioning.</param>
[Experimental("ASPIREAZUREAISERVICES001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public class AzureAIServicesResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    private readonly List<AzureAIServicesDeploymentResource> _deployments = [];

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
    /// Gets the list of deployments of the Azure AI Services resource.
    /// </summary>
    public IReadOnlyList<AzureAIServicesDeploymentResource> Deployments => _deployments;

    internal void AddDeployment(AzureAIServicesDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        _deployments.Add(deployment);
    }

    internal ReferenceExpression GetConnectionString(string deploymentName) =>
        ReferenceExpression.Create($"{ConnectionString};Deployment={deploymentName}");
}
