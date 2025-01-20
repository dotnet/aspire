// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Aspire.Hosting.Azure;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure OpenAI resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Configures the underlying Azure resource using Azure.Provisioning.</param>
public class AzureOpenAIResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    private readonly List<AzureOpenAIDeployment> _deployments = [];

    /// <summary>
    /// Gets the "connectionString" output reference from the Azure OpenAI resource.
    /// </summary>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    /// <summary>
    /// Gets the list of deployments of the Azure OpenAI resource.
    /// </summary>
    public IReadOnlyList<AzureOpenAIDeployment> Deployments => _deployments;

    internal void AddDeployment(AzureOpenAIDeployment deployment)
    {
        ArgumentNullException.ThrowIfNull(deployment);

        _deployments.Add(deployment);
    }
}
