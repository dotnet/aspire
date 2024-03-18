// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// A builder for creating instances of <see cref="DistributedApplication"/>.
/// </summary>
public interface IDistributedApplicationBuilder
{
    /// <inheritdoc cref="HostApplicationBuilder.Configuration" />
    public ConfigurationManager Configuration { get; }

    /// <summary>
    /// Directory of the project where the app host is located. Defaults to the content root if there's no project.
    /// </summary>
    public string AppHostDirectory { get; }

    /// <inheritdoc cref="HostApplicationBuilder.Environment" />
    public IHostEnvironment Environment { get; }

    /// <inheritdoc cref="HostApplicationBuilder.Services" />
    public IServiceCollection Services { get; }

    /// <summary>
    /// Execution context for this invocation of the AppHost.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Gets the collection of resources for the distributed application.
    /// </summary>
    /// <remarks>
    /// This can be mutated by adding more resources, which will update its current view.
    /// </remarks>
    public IResourceCollection Resources { get; }

    /// <summary>
    /// Adds a resource of type <typeparamref name="T"/> to the distributed application.
    /// </summary>
    /// <typeparam name="T">The type of resource to add.</typeparam>
    /// <param name="resource">The resource to add.</param>
    /// <returns>A builder for configuring the added resource.</returns>
    /// <exception cref="DistributedApplicationException">Thrown when a resource with the same name already exists.</exception>
    IResourceBuilder<T> AddResource<T>(T resource) where T : IResource;

    /// <summary>
    /// Creates a new resource builder based on an existing resource.
    /// </summary>
    /// <typeparam name="T">Type of resource.</typeparam>
    /// <param name="resource">An existing resource.</param>
    /// <returns>A resource builder.</returns>
    IResourceBuilder<T> CreateResourceBuilder<T>(T resource) where T : IResource;

    /// <summary>
    /// Builds and returns a new <see cref="DistributedApplication"/> instance. This can only be called once.
    /// </summary>
    /// <returns>A new <see cref="DistributedApplication"/> instance.</returns>
    DistributedApplication Build();
}
