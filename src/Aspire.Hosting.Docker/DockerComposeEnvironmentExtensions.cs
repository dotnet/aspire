// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Docker Compose environment resources to the application model.
/// </summary>
public static class DockerComposeEnvironmentExtensions
{
    /// <summary>
    /// Adds a Docker Compose environment to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the Docker Compose environment resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DockerComposeEnvironmentResource}"/>.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> AddDockerComposeEnvironment(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new DockerComposeEnvironmentResource(name);
        builder.Services.TryAddLifecycleHook<DockerComposeInfrastructure>();
        if (builder.ExecutionContext.IsRunMode)
        {

            // Return a builder that isn't added to the top-level application builder
            // so it doesn't surface as a resource.
            return builder.CreateResourceBuilder(resource);

        }
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Allows setting the properties of a Docker Compose environment resource.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="DockerComposeEnvironmentResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithProperties(this IResourceBuilder<DockerComposeEnvironmentResource> builder, Action<DockerComposeEnvironmentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }

    /// <summary>
    /// Configures the Docker Compose file for the environment resource.
    /// </summary>
    /// <param name="builder"> The Docker compose environment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="ComposeFile"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> ConfigureComposeFile(this IResourceBuilder<DockerComposeEnvironmentResource> builder, Action<ComposeFile> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Resource.ConfigureComposeFile += configure;
        return builder;
    }

    /// <summary>
    /// Configures the Docker Compose environment to include a dashboard for telemetry visualization.
    /// </summary>
    /// <param name="builder">The Docker Compose environment resource builder.</param>
    /// <param name="enabled">Whether to enable the dashboard. Defaults to true.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DockerComposeEnvironmentResource> WithDashboard(this IResourceBuilder<DockerComposeEnvironmentResource> builder, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (enabled && builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            var dashboard = builder.ApplicationBuilder
                .AddContainer("aspire-dashboard", "mcr.microsoft.com/dotnet/nightly/aspire-dashboard")
                .WithHttpEndpoint(targetPort: 18888, name: "dashboard")
                .WithHttpEndpoint(targetPort: 18889, name: "otlp")
                .PublishAsDockerComposeService((_, service) =>
                {
                    service.Restart = "always";
                });

            // Subscribe to BeforeStartEvent to configure OTLP endpoints for all resources with OtlpExporterAnnotation
            builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
            {
                foreach (var resource in e.Model.Resources.OfType<IResourceWithEnvironment>())
                {
                    // Skip the dashboard itself
                    if (resource == dashboard.Resource)
                    {
                        continue;
                    }

                    // Only configure OTLP for resources that have the OtlpExporterAnnotation
                    if (resource.Annotations.OfType<OtlpExporterAnnotation>().Any())
                    {
                        builder.ApplicationBuilder.CreateResourceBuilder(resource).WithEnvironment(c =>
                        {
                            c.EnvironmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = dashboard.GetEndpoint("otlp");
                            c.EnvironmentVariables["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc";
                            c.EnvironmentVariables["OTEL_SERVICE_NAME"] = resource.Name;
                        });
                    }
                }

                return Task.CompletedTask;
            });
        }

        return builder;
    }
}
