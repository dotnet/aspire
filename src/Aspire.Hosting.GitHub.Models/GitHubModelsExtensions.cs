// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.GitHub.Models;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding GitHub Models resources to the application model.
/// </summary>
public static class GitHubModelsExtensions
{
    /// <summary>
    /// Adds a GitHub Models resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with GitHub Models.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GitHubModelsResource> AddGitHubModel(this IDistributedApplicationBuilder builder, [ResourceName] string name, string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var resource = new GitHubModelsResource(name, model);
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Configures the endpoint for the GitHub Models resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="endpoint">The endpoint URL.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithEndpoint(this IResourceBuilder<GitHubModelsResource> builder, string endpoint)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        builder.Resource.Endpoint = endpoint;
        return builder;
    }

    /// <summary>
    /// Configures the API key for the GitHub Models resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="key">The API key.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithApiKey(this IResourceBuilder<GitHubModelsResource> builder, string key)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(key);

        builder.Resource.Key = key;
        return builder;
    }

    /// <summary>
    /// Configures the API key for the GitHub Models resource from a parameter.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelsResource> WithApiKey(this IResourceBuilder<GitHubModelsResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["GITHUB_TOKEN"] = apiKey.Resource;
        }));
    }
}
