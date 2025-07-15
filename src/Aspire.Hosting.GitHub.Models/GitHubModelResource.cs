// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.GitHub.Models;

/// <summary>
/// Represents a GitHub Model resource.
/// </summary>
public class GitHubModelResource : Resource, IResourceWithConnectionString, IResourceWithoutLifetime
{
    internal ParameterResource DefaultKeyParameter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubModelResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="model">The model name.</param>
    /// <param name="organization">The organization.</param>
    /// <param name="key">The key parameter.</param>
    public GitHubModelResource(string name, string model, ParameterResource? organization, ParameterResource key) : base(name)
    {
        Model = model;
        Organization = organization;
        Key = DefaultKeyParameter = key;
    }

    /// <summary>
    /// Gets or sets the model name, e.g., "openai/gpt-4o-mini".
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the organization login associated with the organization to which the request is to be attributed.
    /// </summary>
    /// <remarks>
    /// If set, the token must be attributed to an organization.
    /// </remarks>
    public ParameterResource? Organization { get; set; }

    /// <summary>
    /// Gets or sets the API key (PAT or GitHub App minted token) for accessing GitHub Models.
    /// </summary>
    /// <remarks>
    /// The token must have the <code>models: read</code> permission if using a fine-grained PAT or GitHub App minted token.
    /// </remarks>
    public ParameterResource Key { get; internal set; }

    /// <summary>
    /// Gets the connection string expression for the GitHub Models resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Organization is not null
            ? ReferenceExpression.Create($"Endpoint=https://models.github.ai/orgs/{Organization}/inference;Key={Key};Model={Model};DeploymentId={Model}")
            : ReferenceExpression.Create($"Endpoint=https://models.github.ai/inference;Key={Key};Model={Model};DeploymentId={Model}");
}
