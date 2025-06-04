// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a local Azure AI Foundry resource with endpoints and connection string capabilities.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureAIFoundryLocalResource(string name) : Resource(name), IResourceWithEndpoints, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpointReference;

    /// <summary>
    /// Gets the primary endpoint reference for the resource.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpointReference ??= new(this, PrimaryEndpointName);

    private readonly List<AzureAIFoundryLocalModelResource> _models = [];

    /// <summary>
    /// Gets the list of models associated with the resource.
    /// </summary>
    public IReadOnlyList<AzureAIFoundryLocalModelResource> Models => _models;

    /// <summary>
    /// Gets the connection string expression for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create(
        $"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Host)}"
      );

    /// <summary>
    /// Adds a model resource to the list of models.
    /// </summary>
    /// <param name="modelResource">The model resource to add.</param>
    internal void AddModel(AzureAIFoundryLocalModelResource modelResource) => _models.Add(modelResource);
}
