// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a local model resource for Azure AI Foundry.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="model">The model associated with the resource.</param>
/// <param name="parent">The parent resource to which this model belongs.</param>
public class AzureAIFoundryLocalModelResource(string name, string model, AzureAIFoundryLocalResource parent) : Resource(name), IResourceWithParent<AzureAIFoundryLocalResource>
{
    /// <summary>
    /// Gets the model associated with the resource.
    /// </summary>
    public string Model { get; } = model;

    /// <inheritdoc />
    public AzureAIFoundryLocalResource Parent { get; } = parent;
}
