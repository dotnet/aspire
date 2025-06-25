// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure AI Foundry resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Configures the underlying Azure resource using Azure.Provisioning.</param>
public class AzureAIFoundryResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString,
    IResourceWithEndpoints
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpointReference;

    private readonly List<AzureAIFoundryDeploymentResource> _deployments = [];

    /// <summary>
    /// Gets the "connectionString" output reference from the Azure AI Foundry resource.
    /// </summary>
    public BicepOutputReference AIFoundryApiEndpoint => new("aiFoundryApiEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={AIFoundryApiEndpoint};EndpointAIInference={AIFoundryApiEndpoint}models");

    /// <summary>
    /// Gets the list of deployment resources associated with the Azure AI Foundry.
    /// </summary>
    public IReadOnlyList<AzureAIFoundryDeploymentResource> Deployments => _deployments;

    /// <summary>
    /// Gets or sets a value indicating whether the resource is local.
    /// </summary>
    public bool IsLocal { get; set; }

    /// <summary>
    /// The API key to access Foundry Local
    /// </summary>
    public string? ApiKey { get; internal set; }

    /// <summary>
    /// Gets the primary endpoint reference for the resource.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpointReference ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var csa = CognitiveServicesAccount.FromExisting(this.GetBicepIdentifier());
        csa.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(csa);
        return csa;
    }

    internal void AddDeployment(AzureAIFoundryDeploymentResource deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        _deployments.Add(deployment);
    }

    internal ReferenceExpression GetConnectionString(string deploymentName) =>
        IsLocal
            ? ReferenceExpression.Create($"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Host)};Key={ApiKey};DeploymentId={deploymentName};Model={deploymentName}")
            : ReferenceExpression.Create($"{ConnectionStringExpression};DeploymentId={deploymentName};Model={deploymentName}");
}
