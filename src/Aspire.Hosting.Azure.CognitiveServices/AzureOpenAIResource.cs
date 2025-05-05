// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.Azure;
using Azure.Provisioning.CognitiveServices;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure OpenAI resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Configures the underlying Azure resource using Azure.Provisioning.</param>
public class AzureOpenAIResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    [Obsolete("Use AzureOpenAIDeploymentResource instead.")]
    private readonly List<AzureOpenAIDeployment> _deployments = [];
    private readonly List<AzureOpenAIDeploymentResource> _deploymentResources = [];

    /// <summary>
    /// Gets the "connectionString" output reference from the Azure OpenAI resource.
    /// </summary>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    private BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    internal ReferenceExpression GetConnectionString(string deploymentName) =>
        ReferenceExpression.Create($"{ConnectionString};Deployment={deploymentName}");

    /// <summary>
    /// Gets the list of deployments of the Azure OpenAI resource.
    /// </summary>
    [Obsolete("AzureOpenAIDeployment is deprecated.")]
    public IReadOnlyList<AzureOpenAIDeployment> Deployments => _deployments;

    internal IReadOnlyList<AzureOpenAIDeploymentResource> DeploymentResources => _deploymentResources;

    [Obsolete("AzureOpenAIDeployment is deprecated.")]
    internal void AddDeployment(AzureOpenAIDeployment deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        _deployments.Add(deployment);
    }

    internal void AddDeployment(AzureOpenAIDeploymentResource deployment)
    {
        _deploymentResources.Add(deployment);
    }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var account = CognitiveServicesAccount.FromExisting(this.GetBicepIdentifier());
        account.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(account);
        return account;
    }
}
