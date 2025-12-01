// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a context for generating agent content from a resource.
/// </summary>
[Experimental("ASPIREAGENTCONTENT001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class AgentContentContext
{
    private readonly List<string> _contentParts = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContentContext"/> class.
    /// </summary>
    /// <param name="resource">The resource for which agent content is being generated.</param>
    /// <param name="serviceProvider">The service provider for accessing application services.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public AgentContentContext(IResource resource, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        Resource = resource;
        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the resource for which agent content is being generated.
    /// </summary>
    public IResource Resource { get; }

    /// <summary>
    /// Gets the service provider for accessing application services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Appends text content to the agent content.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void AppendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _contentParts.Add(text);
    }

    /// <summary>
    /// Appends a line of text content to the agent content.
    /// </summary>
    /// <param name="text">The text to append.</param>
    public void AppendLine(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _contentParts.Add(text + Environment.NewLine);
    }

    /// <summary>
    /// Appends an empty line to the agent content.
    /// </summary>
    public void AppendLine()
    {
        _contentParts.Add(Environment.NewLine);
    }

    /// <summary>
    /// Gets the combined agent content text.
    /// </summary>
    /// <returns>The combined content as a single string.</returns>
    internal string GetContent()
    {
        return string.Concat(_contentParts);
    }
}

/// <summary>
/// Represents an annotation that provides agent content for a resource.
/// Agent content is text that can be retrieved by AI agents to understand the resource.
/// </summary>
[Experimental("ASPIREAGENTCONTENT001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class AgentContentAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentContentAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The callback to invoke to generate agent content.</param>
    public AgentContentAnnotation(Func<AgentContentContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = callback;
    }

    /// <summary>
    /// Gets the callback that generates agent content for the resource.
    /// </summary>
    public Func<AgentContentContext, Task> Callback { get; }
}
