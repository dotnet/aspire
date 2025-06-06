// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AI.Foundry.Local;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a local model resource for Azure AI Foundry.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="model">The model associated with the resource.</param>
/// <param name="parent">The parent resource to which this model belongs.</param>
public class AzureAIFoundryLocalModelResource(string name, string model, AzureAIFoundryLocalResource parent) :
    Resource(name),
    IResourceWithParent<AzureAIFoundryLocalResource>,
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the model associated with the resource.
    /// </summary>
    public string Model { get; } = model;

    /// <inheritdoc />
    public AzureAIFoundryLocalResource Parent { get; } = parent;

    /// <summary>
    /// The Connection String for the AI Foundry Local resource including model and deployment info.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create($"{Parent.ConnectionStringExpression};DeploymentId={ModelInfo?.ModelId};Model={ModelInfo?.ModelId}");

    internal ModelInfo? ModelInfo { get; set; }
}
