// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Docker;

/// <summary>
/// Configuration for the dashboard service in a Docker Compose environment.
/// </summary>
public class DashboardConfiguration
{
    /// <summary>
    /// Gets or sets the container image for the dashboard.
    /// </summary>
    public string Image { get; set; } = "mcr.microsoft.com/dotnet/nightly/aspire-dashboard";

    /// <summary>
    /// Gets or sets the restart policy for the dashboard container.
    /// </summary>
    public string Restart { get; set; } = "always";

    /// <summary>
    /// Gets or sets the dashboard UI port.
    /// </summary>
    public int DashboardPort { get; set; } = 18888;

    /// <summary>
    /// Gets or sets the OTLP ingestion port.
    /// </summary>
    public int OtlpPort { get; set; } = 18889;

    /// <summary>
    /// Gets or sets environment variables for the dashboard container.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets additional ports to expose.
    /// </summary>
    public List<string> AdditionalPorts { get; set; } = new();

    /// <summary>
    /// Gets or sets the container name for the dashboard.
    /// </summary>
    public string? ContainerName { get; set; }
}

/// <summary>
/// Represents a Docker Compose environment resource that can host application resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DockerComposeEnvironmentResource"/> class.
/// </remarks>
public class DockerComposeEnvironmentResource : Resource, IComputeEnvironmentResource
{
    /// <summary>
    /// The container registry to use.
    /// </summary>
    public string? DefaultContainerRegistry { get; set; }

    /// <summary>
    /// The name of an existing network to be used.
    /// </summary>
    public string? DefaultNetworkName { get; set; }

    /// <summary>
    /// Determines whether to build container images for the resources in this environment.
    /// </summary>
    public bool BuildContainerImages { get; set; } = true;

    /// <summary>
    /// Determines whether to include an Aspire dashboard for telemetry visualization in this environment.
    /// </summary>
    public bool DashboardEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the dashboard configuration for this environment.
    /// </summary>
    internal DashboardConfiguration? DashboardConfiguration { get; set; }

    internal Action<ComposeFile>? ConfigureComposeFile { get; set; }

    /// <summary>
    /// Gets the collection of environment variables captured from the Docker Compose environment.
    /// These will be populated into a top-level .env file adjacent to the Docker Compose file.
    /// </summary>
    internal Dictionary<string, (string? Description, string? DefaultValue, object? Source)> CapturedEnvironmentVariables { get; } = [];

    internal Dictionary<IResource, DockerComposeServiceResource> ResourceMapping { get; } = new(new ResourceComparer());

    internal PortAllocator PortAllocator { get; } = new();

    /// <param name="name">The name of the Docker Compose environment.</param>
    public DockerComposeEnvironmentResource(string name) : base(name)
    {
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));
    }

    private Task PublishAsync(PublishingContext context)
    {
        var imageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();

        var dockerComposePublishingContext = new DockerComposePublishingContext(
            context.ExecutionContext,
            imageBuilder,
            context.OutputPath,
            context.Logger,
            context.CancellationToken);

        return dockerComposePublishingContext.WriteModelAsync(context.Model, this);
    }

    internal string AddEnvironmentVariable(string name, string? description = null, string? defaultValue = null, object? source = null)
    {
        CapturedEnvironmentVariables[name] = (description, defaultValue, source);

        return $"${{{name}}}";
    }
}
