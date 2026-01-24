// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Represents pipeline summary information to be displayed after pipeline completion.
/// This is a general-purpose key-value collection that pipeline steps can contribute to.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a flexible way for any pipeline step to contribute
/// information to be displayed after pipeline execution. The data is stored as
/// key-value pairs that will be formatted as a table or list in the CLI output.
/// </para>
/// <para>
/// Pipeline steps can add any relevant information such as resource group names,
/// subscription IDs, URLs, namespaces, cluster names, or any other details.
/// </para>
/// <para>
/// The summary is available via the <see cref="Aspire.Hosting.Pipelines.PipelineContext.Summary"/>
/// property and can be accessed from any pipeline step.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a pipeline step, add to the summary
/// public async Task ExecuteAsync(PipelineStepContext context)
/// {
///     // Do work...
///
///     // Add summary items
///     context.PipelineContext.Summary.Add("☁️ Target", "Azure");
///     context.PipelineContext.Summary.Add("📦 Resource Group", "rg-myapp");
///     context.PipelineContext.Summary.Add("🔑 Subscription", "12345678-1234-1234-1234-123456789012");
///     context.PipelineContext.Summary.Add("🌐 Location", "eastus");
/// }
///
/// // Kubernetes example
/// context.PipelineContext.Summary.Add("☸️ Target", "Kubernetes");
/// context.PipelineContext.Summary.Add("📦 Namespace", "production");
/// context.PipelineContext.Summary.Add("🖥️ Cluster", "my-cluster");
///
/// // Docker example
/// context.PipelineContext.Summary.Add("🐳 Target", "Docker");
/// context.PipelineContext.Summary.Add("🌐 Endpoint", "localhost:8080");
/// </code>
/// </example>
[Experimental("ASPIREPIPELINES001", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
public sealed class PipelineSummary
{
    private readonly List<KeyValuePair<string, string>> _items = [];

    /// <summary>
    /// Gets the items in the pipeline summary as a read-only collection.
    /// Items are displayed in the order they were added.
    /// </summary>
    public ReadOnlyCollection<KeyValuePair<string, string>> Items => _items.AsReadOnly();

    /// <summary>
    /// Adds a key-value pair to the pipeline summary.
    /// </summary>
    /// <param name="key">The key or label for the item (e.g., "Resource Group", "Namespace", "URL").</param>
    /// <param name="value">The value for the item.</param>
    public void Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _items.Add(new KeyValuePair<string, string>(key, value));
    }

}
