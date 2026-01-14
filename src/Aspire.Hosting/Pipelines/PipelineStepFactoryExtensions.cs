// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Provides extension methods for adding pipeline steps to resources.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public static class PipelineStepFactoryExtensions
{
    /// <summary>
    /// Adds a pipeline step to the resource that will be executed during deployment.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="factory">A factory function that creates the pipeline step.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineStepFactory<T>(
        this IResourceBuilder<T> builder,
        Func<PipelineStepFactoryContext, PipelineStep> factory) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.WithAnnotation(new PipelineStepAnnotation(factory));
    }

    /// <summary>
    /// Adds a pipeline step to the resource that will be executed during deployment.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="factory">An async factory function that creates the pipeline step.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineStepFactory<T>(
        this IResourceBuilder<T> builder,
        Func<PipelineStepFactoryContext, Task<PipelineStep>> factory) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.WithAnnotation(new PipelineStepAnnotation(factory));
    }

    /// <summary>
    /// Adds multiple pipeline steps to the resource that will be executed during deployment.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="factory">A factory function that creates multiple pipeline steps.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineStepFactory<T>(
        this IResourceBuilder<T> builder,
        Func<PipelineStepFactoryContext, IEnumerable<PipelineStep>> factory) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.WithAnnotation(new PipelineStepAnnotation(factory));
    }

    /// <summary>
    /// Adds multiple pipeline steps to the resource that will be executed during deployment.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="factory">An async factory function that creates multiple pipeline steps.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineStepFactory<T>(
        this IResourceBuilder<T> builder,
        Func<PipelineStepFactoryContext, Task<IEnumerable<PipelineStep>>> factory) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.WithAnnotation(new PipelineStepAnnotation(factory));
    }

    /// <summary>
    /// Registers a callback to be executed during the pipeline configuration phase,
    /// allowing modification of step dependencies and relationships.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">The callback function to execute during the configuration phase.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineConfiguration<T>(
        this IResourceBuilder<T> builder,
        Func<PipelineConfigurationContext, Task> callback) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithAnnotation(new PipelineConfigurationAnnotation(callback));
    }

    /// <summary>
    /// Registers a callback to be executed during the pipeline configuration phase,
    /// allowing modification of step dependencies and relationships.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">The callback function to execute during the configuration phase.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<T> WithPipelineConfiguration<T>(
        this IResourceBuilder<T> builder,
        Action<PipelineConfigurationContext> callback) where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(callback);

        return builder.WithAnnotation(new PipelineConfigurationAnnotation(callback));
    }
}
