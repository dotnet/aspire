// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a local model resource for Azure AI Foundry.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The parent resource to which this model belongs.</param>
public class AzureAIFoundryLocalModelResource(string name, AzureAIFoundryLocalResource parent) :
    Resource(name),
    IResourceWithParent<AzureAIFoundryLocalResource>,
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the model id associated with the resource. e.g., "Phi-3.5-mini-instruct-generic-cpu"
    /// </summary>
    public string? ModelId { get; set; }

    /// <inheritdoc />
    public AzureAIFoundryLocalResource Parent { get; } = parent;

    /// <summary>
    /// The Connection String for the AI Foundry Local resource including model and deployment info.
    /// </summary>
    /// <remarks>
    /// If contains DeploymentId for Aspire.Azure.AI.Inference and Model for Aspire.OpenAI
    /// </remarks>
    public ReferenceExpression ConnectionStringExpression
        => ReferenceExpression.Create($"{Parent.ConnectionStringExpression};DeploymentId={ModelId};Model={ModelId}");
}
