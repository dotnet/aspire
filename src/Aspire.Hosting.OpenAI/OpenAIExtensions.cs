// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding OpenAI Model resources to the application model.
/// </summary>
public static class OpenAIExtensions
{
    /// <summary>
    /// Adds an OpenAI Model resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="model">The model name to use with OpenAI.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OpenAIModelResource> AddOpenAIModel(this IDistributedApplicationBuilder builder, [ResourceName] string name, string model)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(model);

        var defaultApiKeyParameter = builder.AddParameter($"{name}-openai-apikey", () =>
            builder.Configuration[$"Parameters:{name}-openai-apikey"] ??
            Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
            throw new MissingParameterValueException($"OpenAI API key parameter '{name}-openai-apikey' is missing and OPENAI_API_KEY environment variable is not set."),
            secret: true);
        defaultApiKeyParameter.Resource.Description = """
            The API key used to authenticate requests to the OpenAI API.
            You can obtain an API key from the [OpenAI API Keys page](https://platform.openai.com/api-keys).
            """;
        defaultApiKeyParameter.Resource.EnableDescriptionMarkdown = true;
        var resource = new OpenAIModelResource(name, model, defaultApiKeyParameter.Resource);

        defaultApiKeyParameter.WithParentRelationship(resource);

        return builder.AddResource(resource)
            .WithInitialState(new()
            {
                ResourceType = "OpenAIModel",
                CreationTimeStamp = DateTime.UtcNow,
                State = KnownResourceStates.Waiting,
                Properties =
                [
                    new(CustomResourceKnownProperties.Source, "OpenAI")
                ]
            })
            .OnInitializeResource(async (r, evt, ct) =>
            {
                // Connection string resolution is dependent on parameters being resolved
                // We use this to wait for the parameters to be resolved before we can compute the connection string.
                var cs = await r.ConnectionStringExpression.GetValueAsync(ct).ConfigureAwait(false);

                // Publish the update with the connection string value and the state as running.
                // This will allow health checks to start running.
                await evt.Notifications.PublishUpdateAsync(r, s => s with
                {
                    State = KnownResourceStates.Running,
                    Properties = [.. s.Properties, new(CustomResourceKnownProperties.ConnectionString, cs) { IsSensitive = true }]
                }).ConfigureAwait(false);

                // Publish the connection string available event for other resources that may depend on this resource.
                await evt.Eventing.PublishAsync(new ConnectionStringAvailableEvent(r, evt.Services), ct)
                                  .ConfigureAwait(false);
            });
    }

    /// <summary>
    /// Configures the API key for the OpenAI Model resource from a parameter.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="apiKey">The API key parameter.</param>
    /// <returns>The resource builder.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided parameter is not marked as secret.</exception>
    public static IResourceBuilder<OpenAIModelResource> WithApiKey(this IResourceBuilder<OpenAIModelResource> builder, IResourceBuilder<ParameterResource> apiKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(apiKey);

        if (!apiKey.Resource.Secret)
        {
            throw new ArgumentException("The API key parameter must be marked as secret. Use AddParameter with secret: true when creating the parameter.", nameof(apiKey));
        }

        // Remove the existing parameter if it's the default one
        if (builder.Resource.DefaultKeyParameter == builder.Resource.Key)
        {
            builder.ApplicationBuilder.Resources.Remove(builder.Resource.Key);
        }

        builder.Resource.Key = apiKey.Resource;

        return builder;
    }

    /// <summary>
    /// Adds a health check to the OpenAI Model resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a health check that verifies the OpenAI endpoint is accessible,
    /// the API key is valid, and the specified model is available. The health check will:
    /// </para>
    /// <list type="bullet">
    /// <item>Return <see cref="HealthStatus.Healthy"/> when the endpoint returns HTTP 200</item>
    /// <item>Return <see cref="HealthStatus.Unhealthy"/> with details when the API key is invalid (HTTP 401)</item>
    /// <item>Return <see cref="HealthStatus.Unhealthy"/> with error details when the model is unknown (HTTP 404)</item>
    /// </list>
    /// <para>
    /// Because health checks are included in the rate limit of the OpenAI API,
    /// it is recommended to use this health check sparingly, such as when you are having issues understanding the reason
    /// the model is not working as expected. Furthermore, the health check will run a single time per application instance.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<OpenAIModelResource> WithHealthCheck(this IResourceBuilder<OpenAIModelResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var healthCheckKey = $"{builder.Resource.Name}_check";
        OpenAIHealthCheck? healthCheck = null;

        // Ensure IHttpClientFactory is available by registering HTTP client services
        builder.ApplicationBuilder.Services.AddHttpClient();

        // Register the health check
        builder.ApplicationBuilder.Services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                healthCheckKey,
                sp =>
                {
                    // Cache the health check instance so we can reuse its result in order to avoid multiple API calls
                    // that would exhaust the rate limit.

                    if (healthCheck is not null)
                    {
                        return healthCheck;
                    }

                    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenAIHealthCheck");

                    var resource = builder.Resource;

                    return healthCheck = new OpenAIHealthCheck(httpClient, async () => await resource.ConnectionStringExpression.GetValueAsync(default).ConfigureAwait(false));
                },
                failureStatus: default,
                tags: default,
                timeout: default));

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }
}
