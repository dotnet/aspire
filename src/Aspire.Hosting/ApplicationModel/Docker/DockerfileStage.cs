// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Represents a stage within a multi-stage Dockerfile.
/// </summary>
public class DockerfileStage : DockerfileStatement
{
    private readonly List<DockerfileStatement> _statements = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerfileStage"/> class.
    /// </summary>
    /// <param name="stageName">The optional stage name.</param>
    /// <param name="imageReference">The base image reference.</param>
    public DockerfileStage(string? stageName, string imageReference)
    {
        StageName = stageName;
        
        // Add the FROM statement as the first statement
        _statements.Add(new DockerfileFromStatement(imageReference, stageName));
    }

    /// <summary>
    /// Gets the name of the stage.
    /// </summary>
    public string? StageName { get; }

    /// <summary>
    /// Gets the statements for this stage.
    /// </summary>
    public IList<DockerfileStatement> Statements => _statements;

    /// <summary>
    /// Adds a WORKDIR statement to set the working directory.
    /// </summary>
    /// <param name="path">The working directory path.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage WorkDir(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _statements.Add(new DockerfileWorkDirStatement(path));
        return this;
    }

    /// <summary>
    /// Adds a RUN statement to execute a command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage Run(string command)
    {
        ArgumentException.ThrowIfNullOrEmpty(command);
        _statements.Add(new DockerfileRunStatement(command));
        return this;
    }

    /// <summary>
    /// Adds a COPY statement to copy files from the build context.
    /// </summary>
    /// <param name="source">The source path or pattern.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage Copy(string source, string destination)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentException.ThrowIfNullOrEmpty(destination);
        _statements.Add(new DockerfileCopyStatement(source, destination));
        return this;
    }

    /// <summary>
    /// Adds a COPY statement to copy files from another stage.
    /// </summary>
    /// <param name="stage">The source stage name.</param>
    /// <param name="source">The source path in the stage.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage CopyFrom(string stage, string source, string destination)
    {
        ArgumentException.ThrowIfNullOrEmpty(stage);
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentException.ThrowIfNullOrEmpty(destination);
        _statements.Add(new DockerfileCopyFromStatement(stage, source, destination));
        return this;
    }

    /// <summary>
    /// Adds an ENV statement to set an environment variable.
    /// </summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="value">The environment variable value.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage Env(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _statements.Add(new DockerfileEnvStatement(name, value));
        return this;
    }

    /// <summary>
    /// Adds an EXPOSE statement to expose a port.
    /// </summary>
    /// <param name="port">The port number to expose.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage Expose(int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        _statements.Add(new DockerfileExposeStatement(port));
        return this;
    }

    /// <summary>
    /// Adds a CMD statement to set the default command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The current stage.</returns>
    public DockerfileStage Cmd(string[] command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Length == 0)
        {
            throw new ArgumentException("Command array cannot be empty.", nameof(command));
        }
        _statements.Add(new DockerfileCmdStatement(command));
        return this;
    }

    /// <inheritdoc />
    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        foreach (var statement in _statements)
        {
            await statement.WriteStatementAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}