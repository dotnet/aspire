// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Docker Compose service resources.
/// </summary>
public static class DockerComposeServiceExtensions
{
    /// <summary>
    /// Publishes the specified resource as a Docker Compose service.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">The configuration action for the Docker Compose service.</param>
    /// <returns>The updated resource builder.</returns>
    /// <remarks>
    /// This method checks if the application is in publish mode. If it is, it adds a customization annotation
    /// that will be applied by the DockerComposeInfrastructure when generating the Docker Compose service.
    /// <example>
    /// <code>
    /// builder.AddContainer("redis", "redis:alpine").PublishAsDockerComposeService((resource, service) =>
    /// {
    ///     service.Name = "redis";
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> PublishAsDockerComposeService<T>(this IResourceBuilder<T> builder, Action<DockerComposeServiceResource, Service> configure)
        where T : IComputeResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithAnnotation(new DockerComposeServiceCustomizationAnnotation(configure));

        return builder;
    }

    /// <summary>
    /// Creates a placeholder for an environment variable in the Docker Compose file.
    /// </summary>
    /// <param name="manifestExpressionProvider">The manifest expression provider.</param>
    /// <param name="dockerComposeService">The Docker Compose service resource to associate the environment variable with.</param>
    /// <returns>A string representing the environment variable placeholder in Docker Compose syntax (e.g., <c>${ENV_VAR}</c>).</returns>
    public static string AsEnvironmentPlaceholder(this IManifestExpressionProvider manifestExpressionProvider, DockerComposeServiceResource dockerComposeService)
    {
        var env = manifestExpressionProvider.ValueExpression.Replace("{", "")
                 .Replace("}", "")
                 .Replace(".", "_")
                 .Replace("-", "_")
                 .ToUpperInvariant();

        return dockerComposeService.Parent.AddEnvironmentVariable(
            env,
            source: manifestExpressionProvider
        );
    }

    /// <summary>
    /// Creates a Docker Compose environment variable placeholder for the specified <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="builder">The resource builder for the parameter resource.</param>
    /// <param name="dockerComposeService">The Docker Compose service resource to associate the environment variable with.</param>
    /// <returns>A string representing the environment variable placeholder in Docker Compose syntax (e.g., <c>${ENV_VAR}</c>).</returns>
    public static string AsEnvironmentPlaceholder(this IResourceBuilder<ParameterResource> builder, DockerComposeServiceResource dockerComposeService)
    {
        return builder.Resource.AsEnvironmentPlaceholder(dockerComposeService);
    }

    /// <summary>
    /// Creates a Docker Compose environment variable placeholder for this <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="parameter">The parameter resource for which to create the environment variable placeholder.</param>
    /// <param name="dockerComposeService">The Docker Compose service resource to associate the environment variable with.</param>
    /// <returns>A string representing the environment variable placeholder in Docker Compose syntax (e.g., <c>${ENV_VAR}</c>).</returns>
    public static string AsEnvironmentPlaceholder(this ParameterResource parameter, DockerComposeServiceResource dockerComposeService)
    {
        // Placeholder for resolving the actual parameter value
        // https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#interpolation-syntax

        var env = parameter.Name.ToUpperInvariant().Replace("-", "_");

        // Treat secrets as environment variable placeholders as for now
        // this doesn't handle generation of parameter values with defaults
        return dockerComposeService.Parent.AddEnvironmentVariable(
            env,
            description: $"Parameter {parameter.Name}",
            defaultValue: parameter.Secret || parameter.Default is null ? null : parameter.Value,
            source: parameter
        );
    }
}
