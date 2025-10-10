// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides contextual information and services for the deploying process of a distributed application.
/// </summary>
/// <param name="model">The distributed application model to be deployed.</param>
/// <param name="executionContext">The execution context for the distributed application.</param>
/// <param name="serviceProvider">The service provider for dependency resolution.</param>
/// <param name="logger">The logger for deploying operations.</param>
/// <param name="cancellationToken">The cancellation token for the deploying operation.</param>
/// <param name="outputPath">The output path for deployment artifacts.</param>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class DeployingContext(
    DistributedApplicationModel model,
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider,
    ILogger logger,
    CancellationToken cancellationToken,
    string? outputPath)
{
    private IPublishingActivityReporter? _activityReporter;
    private readonly Dictionary<string, object> _pipelineOutputs = new();

    /// <summary>
    /// Gets the distributed application model to be deployed.
    /// </summary>
    public DistributedApplicationModel Model { get; } = model;

    /// <summary>
    /// Gets the execution context for the distributed application.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext;

    /// <summary>
    /// Gets the service provider for dependency resolution.
    /// </summary>
    public IServiceProvider Services { get; } = serviceProvider;

    /// <summary>
    /// Gets the activity reporter for deploying activities.
    /// </summary>
    public IPublishingActivityReporter ActivityReporter => _activityReporter ??=
        Services.GetRequiredService<IPublishingActivityReporter>();

    /// <summary>
    /// Gets the logger for deploying operations.
    /// </summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// Gets the cancellation token for the deploying operation.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// Gets the output path for deployment artifacts.
    /// </summary>
    public string? OutputPath { get; } = outputPath;

    /// <summary>
    /// Sets an output value that can be consumed by dependent pipeline steps.
    /// </summary>
    /// <typeparam name="T">The type of the output value.</typeparam>
    /// <param name="key">The key to identify the output.</param>
    /// <param name="value">The value to store.</param>
    public void SetPipelineOutput<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _pipelineOutputs[key] = value;
    }

    /// <summary>
    /// Attempts to retrieve an output value set by a previous pipeline step.
    /// </summary>
    /// <typeparam name="T">The expected type of the output value.</typeparam>
    /// <param name="key">The key identifying the output.</param>
    /// <param name="value">The retrieved value if found.</param>
    /// <returns>True if the output was found and is of the expected type; otherwise, false.</returns>
    public bool TryGetPipelineOutput<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (_pipelineOutputs.TryGetValue(key, out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Retrieves an output value set by a previous pipeline step.
    /// </summary>
    /// <typeparam name="T">The expected type of the output value.</typeparam>
    /// <param name="key">The key identifying the output.</param>
    /// <returns>The output value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the output is not found or is not of the expected type.</exception>
    public T GetPipelineOutput<T>(string key)
    {
        if (!TryGetPipelineOutput<T>(key, out var value))
        {
            throw new InvalidOperationException(
                $"Pipeline output '{key}' not found or is not of type {typeof(T).Name}");
        }
        return value;
    }
}
