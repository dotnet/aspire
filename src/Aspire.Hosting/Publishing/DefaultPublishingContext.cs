// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

/// <summary>
/// 
/// </summary>
/// <param name="model"></param>
/// <param name="executionContext"></param>
/// <param name="logger"></param>
/// <param name="serviceProvider"></param>
/// <param name="cancellationToken"></param>
/// <param name="outputPath"></param>
public sealed class DefaultPublishingContext(
    DistributedApplicationModel model,
    DistributedApplicationExecutionContext executionContext,
    IServiceProvider serviceProvider,
    ILogger logger,
    CancellationToken cancellationToken,
    string outputPath)
{
    /// <summary>
    /// 
    /// </summary>
    public DistributedApplicationModel Model { get; } = model;

    /// <summary>
    /// 
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext;

    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider Services { get; } = serviceProvider;

    /// <summary>
    /// 
    /// </summary>
    public ILogger Logger { get; } = logger;

    /// <summary>
    /// 
    /// </summary>
    public string OutputPath { get; } = outputPath;

    /// <summary>
    /// 
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task WriteModelAsync(DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetLastAnnotation<DefaultPublishingCallbackAnnotation>(out var annotation))
            {
                await annotation.Callback(this).ConfigureAwait(false);
            }
        }
    }
}
