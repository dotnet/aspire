// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// An annotation that registers a callback to execute during the pipeline's second pass,
/// allowing modification of step dependencies and relationships.
/// </summary>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class PipelinePassAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback function to execute during the second pass.
    /// </summary>
    public Func<PipelinePassContext, Task> Callback { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinePassAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The callback function to execute during the second pass.</param>
    public PipelinePassAnnotation(Func<PipelinePassContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = callback;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinePassAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The synchronous callback function to execute during the second pass.</param>
    public PipelinePassAnnotation(Action<PipelinePassContext> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = (context) =>
        {
            callback(context);
            return Task.CompletedTask;
        };
    }
}
