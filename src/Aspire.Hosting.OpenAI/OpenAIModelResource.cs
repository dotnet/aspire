// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Represents an OpenAI Model resource.
/// </summary>
public class OpenAIModelResource : Resource, IResourceWithConnectionString
{
    internal ParameterResource DefaultKeyParameter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIModelResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="model">The model name.</param>
    /// <param name="key">The key parameter.</param>
    public OpenAIModelResource(string name, string model, ParameterResource key) : base(name)
    {
        Model = model;
        Key = DefaultKeyParameter = key;
    }

    /// <summary>
    /// Gets or sets the model name, e.g., "gpt-4o-mini".
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the API key for accessing OpenAI.
    /// </summary>
    public ParameterResource Key { get; internal set; }

    /// <summary>
    /// Gets the connection string expression for the OpenAI Model resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint=https://api.openai.com/v1;Key={Key};Model={Model}");
}
