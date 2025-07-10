// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.GitHub.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding GitHub Models resources to the application model.
/// </summary>
public static class GitHubModelsExtensions
{
    /// <summary>
    /// Adds a GitHub Model resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with GitHub Models.</param>
    /// <param name="organization">The organization login associated with the organization to which the request is to be attributed.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<GitHubModelResource> AddGitHubModel(this IDistributedApplicationBuilder builder, [ResourceName] string name, string model, IResourceBuilder<ParameterResource>? organization = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var resource = new GitHubModelResource(name, model, organization?.Resource);

        return builder.AddResource(resource)
            .WithHealthCheck()
            .WithInitialState(new()
            {
                ResourceType = "GitHubModel",
                CreationTimeStamp = DateTime.UtcNow,
                State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success),
                Properties =
                    [
                        new(CustomResourceKnownProperties.Source, "Github Models")
                    ]
            });
    }

    /// <summary>
    /// Configures the API key for the GitHub Model resource from a parameter.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<GitHubModelResource> WithApiKey(this IResourceBuilder<GitHubModelResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        builder.Resource.Key = apiKey.Resource;

        return builder;
    }

    /// <summary>
    /// Adds a health check to the GitHub Model resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a health check that verifies the GitHub Models endpoint is accessible,
    /// the API key is valid, and the specified model is available. The health check will:
    /// </para>
    /// <list type="bullet">
    /// <item>Return <see cref="HealthStatus.Healthy"/> when the endpoint returns HTTP 200</item>
    /// <item>Return <see cref="HealthStatus.Unhealthy"/> with details when the API key is invalid (HTTP 401)</item>
    /// <item>Return <see cref="HealthStatus.Unhealthy"/> with error details when the model is unknown (HTTP 404)</item>
    /// </list>
    /// </remarks>
    internal static IResourceBuilder<GitHubModelResource> WithHealthCheck(this IResourceBuilder<GitHubModelResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var healthCheckKey = $"{builder.Resource.Name}_github_models_check";

        // Register the health check
        builder.ApplicationBuilder.Services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                healthCheckKey,
                sp =>
                {
                    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("GitHubModelsHealthCheck");

                    var resource = builder.Resource;

                    return new GitHubModelsHealthCheck(httpClient, async () => await resource.ConnectionStringExpression.GetValueAsync(default).ConfigureAwait(false));
                },
                failureStatus: default,
                tags: default,
                timeout: default));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }
}
