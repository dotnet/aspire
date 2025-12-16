// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.CognitiveServices;

namespace Aspire.Hosting.Azure.CognitiveServices;

/// <summary>
/// An Aspire wrapper around the Azure provisioning CognitiveServicesAccount resource.
/// </summary>
public class AzureCognitiveServicesAccountResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureResourceManagerAspireResource<CognitiveServicesAccount>(name, configureInfrastructure), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the Azure Cognitive Services account resource.
    ///
    /// The connection string for this resource is the endpoint URL for the AI Foundry API
    /// using the "default" project.
    /// </summary>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    /// <summary>
    /// Azure Resource ID output reference.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the name of the environment variable to use for the connection string.
    /// </summary>
    public string ConnectionStringEnvironmentVariable { get; } = "AZURE_AI_PROJECT_ENDPOINT";

    /// <inheritdoc/>
    public override CognitiveServicesAccount FromExisting(string bicepIdentifier)
    {
        return CognitiveServicesAccount.FromExisting(bicepIdentifier);
    }

    /// <inheritdoc/>
    public override void SetName(CognitiveServicesAccount provisionableResource, BicepValue<string> name)
    {
        provisionableResource.Name = name;
    }
}
