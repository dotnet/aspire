// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.GitHub.Models;

/// <summary>
/// Represents a GitHub Model resource.
/// </summary>
public class GitHubModelResource : Resource, IResourceWithConnectionString, IResourceWithoutLifetime
{
    internal const string GitHubModelsEndpoint = "https://models.github.ai/inference";

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubModelResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="model">The model name.</param>
    public GitHubModelResource(string name, string model) : base(name)
    {
        Model = model;
    }

    /// <summary>
    /// Gets or sets the model name, e.g., "openai/gpt-4o-mini".
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the API key for accessing GitHub Models.
    /// </summary>
    /// <remarks>
    /// If not set, the value will be retrieved from the environment variable GITHUB_TOKEN.
    /// </remarks>
    public ParameterResource Key { get; set; } = new ParameterResource("github-api-key", p => Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty, secret: true);

    /// <summary>
    /// Gets the connection string expression for the GitHub Models resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"Endpoint={GitHubModelsEndpoint};Key={Key};Model={Model};DeploymentId={Model}");
}
