// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Implementation of <see cref="IContainerStageBuilder"/> for building container file stages.
/// </summary>
internal class ContainerStageBuilder : IContainerStageBuilder
{
    private readonly List<IContainerfileStatement> _statements = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerStageBuilder"/> class.
    /// </summary>
    /// <param name="stageName">The optional stage name.</param>
    /// <param name="imageReference">The base image reference.</param>
    public ContainerStageBuilder(string? stageName, string imageReference)
    {
        StageName = stageName;
        
        // Add the FROM statement as the first statement
        _statements.Add(new FromStatement(imageReference, stageName));
    }

    /// <inheritdoc />
    public string? StageName { get; }

    /// <inheritdoc />
    public IList<IContainerfileStatement> Statements => _statements;

    /// <inheritdoc />
    public IContainerStageBuilder WorkDir(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _statements.Add(new WorkDirStatement(path));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder Run(string command)
    {
        ArgumentException.ThrowIfNullOrEmpty(command);
        _statements.Add(new RunStatement(command));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder Copy(string source, string destination)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentException.ThrowIfNullOrEmpty(destination);
        _statements.Add(new CopyStatement(source, destination));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder CopyFrom(string stage, string source, string destination)
    {
        ArgumentException.ThrowIfNullOrEmpty(stage);
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentException.ThrowIfNullOrEmpty(destination);
        _statements.Add(new CopyFromStatement(stage, source, destination));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder Env(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(value);
        _statements.Add(new EnvStatement(name, value));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder Expose(int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        _statements.Add(new ExposeStatement(port));
        return this;
    }

    /// <inheritdoc />
    public IContainerStageBuilder Cmd(string[] command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Length == 0)
        {
            throw new ArgumentException("Command array cannot be empty.", nameof(command));
        }
        _statements.Add(new CmdStatement(command));
        return this;
    }
}