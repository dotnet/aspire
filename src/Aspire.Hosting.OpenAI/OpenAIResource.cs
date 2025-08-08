// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.OpenAI;

/// <summary>
/// Represents a logical OpenAI account/configuration that can host one or more <see cref="OpenAIModelResource"/> children.
/// </summary>
public sealed class OpenAIResource : Resource, IResourceWithConnectionString
{
    internal ParameterResource DefaultKeyParameter { get; }
    private const string DefaultEndpoint = "https://api.openai.com/v1";

    /// <summary>
    /// Gets or sets the service endpoint base URI for OpenAI-compatible services.
    /// Defaults to https://api.openai.com/v1.
    /// </summary>
    public string Endpoint { get; internal set; } = DefaultEndpoint;

    /// <summary>
    /// Creates a new <see cref="OpenAIResource"/>.
    /// </summary>
    /// <param name="name">Resource name.</param>
    /// <param name="key">API key parameter for OpenAI.</param>
    public OpenAIResource(string name, ParameterResource key) : base(name)
    {
        Key = DefaultKeyParameter = key;
    }

    /// <summary>
    /// Gets or sets the API key for accessing OpenAI.
    /// </summary>
    public ParameterResource Key { get; internal set; }

    /// <summary>
    /// Gets the base connection string for the OpenAI account.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={Endpoint};Key={Key}");
}
