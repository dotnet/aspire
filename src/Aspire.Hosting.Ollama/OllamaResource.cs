// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Ollama;

/// <summary>
/// A resource that represents an Ollama server
/// </summary>
public class OllamaResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";
    internal bool EnableGpu { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="enableGpu">Whether or not to enable GPU support.</param>
    public OllamaResource(string name, bool enableGpu = false) : base(name) => EnableGpu = enableGpu;

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the http endpoint for the Ollama database.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Ollama http endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create(
            $"{PrimaryEndpoint.Property(EndpointProperty.Url)}");

    private readonly List<string> _models = new List<string>();

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the model name.
    /// </summary>
    public List<string> Models => _models;

    internal void AddModel(string modelName)
    {
        _models.Add(modelName);
    }
}
