// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a local model resource for Azure AI Foundry.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="model">The model associated with the resource.</param>
/// <param name="parent">The parent resource to which this model belongs.</param>
public class AzureAIFoundryLocalModelResource(string name, string model, AzureAIFoundryLocalResource parent) : Resource(name), IResourceWithConnectionString, IResourceWithParent<AzureAIFoundryLocalResource>
{
    /// <summary>
    /// Gets the model associated with the resource.
    /// </summary>
    public string Model { get; } = model;

    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public string? ModelId { get; internal set; }

    /// <inheritdoc />
    public AzureAIFoundryLocalResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"{Parent};ModelName={Model};ModelId={ModelId}");
}
