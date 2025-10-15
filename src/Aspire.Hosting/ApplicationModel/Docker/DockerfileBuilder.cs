// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Builder for creating Dockerfiles programmatically.
/// </summary>
[Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class DockerfileBuilder
{
    private readonly List<DockerfileStage> _stages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerfileBuilder"/> class.
    /// </summary>
    public DockerfileBuilder()
    {
    }

    /// <summary>
    /// Gets the stages in this Dockerfile.
    /// </summary>
    public IReadOnlyList<DockerfileStage> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Adds a FROM statement to start a new named stage.
    /// </summary>
    /// <param name="image">The image reference (e.g., 'node:18' or 'alpine:latest').</param>
    /// <param name="stageName">The stage name for multi-stage builds.</param>
    /// <returns>A stage builder for the new stage.</returns>
    [Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public DockerfileStage From(string image, string stageName)
    {
        ArgumentException.ThrowIfNullOrEmpty(image);
        ArgumentException.ThrowIfNullOrEmpty(stageName);

        var stageBuilder = new DockerfileStage(stageName, image);
        _stages.Add(stageBuilder);
        return stageBuilder;
    }

    /// <summary>
    /// Adds a FROM statement to start a new stage.
    /// </summary>
    /// <param name="image">The image reference (e.g., 'node:18' or 'alpine:latest').</param>
    /// <returns>A stage builder for the new stage.</returns>
    [Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public DockerfileStage From(string image)
    {
        ArgumentException.ThrowIfNullOrEmpty(image);

        var stageBuilder = new DockerfileStage(null, image);
        _stages.Add(stageBuilder);
        return stageBuilder;
    }

    /// <summary>
    /// Writes the Dockerfile content to the specified <see cref="StreamWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="StreamWriter"/> to write to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public async Task WriteAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        foreach (var stage in _stages)
        {
            await stage.WriteStatementAsync(writer, cancellationToken).ConfigureAwait(false);
            
            // Add a blank line between stages
            if (stage != _stages.Last())
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
        }
        
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}