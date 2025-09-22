// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Builder for creating container files (e.g., Dockerfiles) programmatically.
/// </summary>
public class ContainerfileBuilder
{
    private readonly ContainerDialect _dialect;
    private readonly List<IContainerStageBuilder> _stages = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerfileBuilder"/> class.
    /// </summary>
    /// <param name="dialect">The container file dialect to use.</param>
    public ContainerfileBuilder(ContainerDialect dialect)
    {
        _dialect = dialect;
    }

    /// <summary>
    /// Gets the container file dialect.
    /// </summary>
    public ContainerDialect Dialect => _dialect;

    /// <summary>
    /// Gets the stages in this container file.
    /// </summary>
    public IReadOnlyList<IContainerStageBuilder> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Adds a FROM statement to start a new stage.
    /// </summary>
    /// <param name="repository">The image repository.</param>
    /// <param name="tag">The image tag.</param>
    /// <param name="stage">The optional stage name for multi-stage builds.</param>
    /// <returns>A stage builder for the new stage.</returns>
    public IContainerStageBuilder From(string repository, string? tag = null, string? stage = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(repository);

        var imageReference = tag is not null ? $"{repository}:{tag}" : repository;
        var stageBuilder = new ContainerStageBuilder(stage, imageReference);
        _stages.Add(stageBuilder);
        return stageBuilder;
    }

    /// <summary>
    /// Writes the container file content to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public async Task WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        foreach (var stage in _stages)
        {
            await WriteStageAsync(stream, stage, cancellationToken).ConfigureAwait(false);
            
            // Add a blank line between stages
            if (stage != _stages.Last())
            {
                await WriteLineAsync(stream, "", cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task WriteStageAsync(Stream stream, IContainerStageBuilder stage, CancellationToken cancellationToken)
    {
        foreach (var statement in stage.Statements)
        {
            await statement.WriteStatementAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteLineAsync(Stream stream, string content, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(content + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}