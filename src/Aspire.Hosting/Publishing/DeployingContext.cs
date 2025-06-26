// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
    string outputPath)
{
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
    public string OutputPath { get; } = outputPath;

    /// <summary>
    /// Invokes deploying callbacks for each resource in the provided distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model whose resources will be processed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task WriteModelAsync(DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<DeployingCallbackAnnotation>(out var annotation))
            {
                await annotation.Callback(this).ConfigureAwait(false);
            }
        }
    }
}
