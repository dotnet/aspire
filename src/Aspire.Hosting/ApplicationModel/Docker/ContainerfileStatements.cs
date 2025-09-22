// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Represents a FROM statement in a container file.
/// </summary>
internal class FromStatement : IContainerfileStatement
{
    private readonly string _imageReference;
    private readonly string? _stageName;

    public FromStatement(string imageReference, string? stageName = null)
    {
        _imageReference = imageReference;
        _stageName = stageName;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = _stageName is not null 
            ? $"FROM {_imageReference} AS {_stageName}" 
            : $"FROM {_imageReference}";
        
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a WORKDIR statement in a container file.
/// </summary>
internal class WorkDirStatement : IContainerfileStatement
{
    private readonly string _path;

    public WorkDirStatement(string path)
    {
        _path = path;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"WORKDIR {_path}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a RUN statement in a container file.
/// </summary>
internal class RunStatement : IContainerfileStatement
{
    private readonly string _command;

    public RunStatement(string command)
    {
        _command = command;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"RUN {_command}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a COPY statement in a container file.
/// </summary>
internal class CopyStatement : IContainerfileStatement
{
    private readonly string _source;
    private readonly string _destination;

    public CopyStatement(string source, string destination)
    {
        _source = source;
        _destination = destination;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"COPY {_source} {_destination}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a COPY --from statement in a container file.
/// </summary>
internal class CopyFromStatement : IContainerfileStatement
{
    private readonly string _stage;
    private readonly string _source;
    private readonly string _destination;

    public CopyFromStatement(string stage, string source, string destination)
    {
        _stage = stage;
        _source = source;
        _destination = destination;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"COPY --from={_stage} {_source} {_destination}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents an ENV statement in a container file.
/// </summary>
internal class EnvStatement : IContainerfileStatement
{
    private readonly string _name;
    private readonly string _value;

    public EnvStatement(string name, string value)
    {
        _name = name;
        _value = value;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"ENV {_name}={_value}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents an EXPOSE statement in a container file.
/// </summary>
internal class ExposeStatement : IContainerfileStatement
{
    private readonly int _port;

    public ExposeStatement(int port)
    {
        _port = port;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"EXPOSE {_port}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a CMD statement in a container file.
/// </summary>
internal class CmdStatement : IContainerfileStatement
{
    private readonly string[] _command;

    public CmdStatement(string[] command)
    {
        _command = command;
    }

    public async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var commandJson = JsonSerializer.Serialize(_command);
        var statement = $"CMD {commandJson}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}