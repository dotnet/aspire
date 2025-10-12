// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Represents a FROM statement in a Dockerfile.
/// </summary>
internal class DockerfileFromStatement : DockerfileStatement
{
    private readonly string _imageReference;
    private readonly string? _stageName;

    public DockerfileFromStatement(string imageReference, string? stageName = null)
    {
        _imageReference = imageReference;
        _stageName = stageName;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = _stageName is not null 
            ? $"FROM {_imageReference} AS {_stageName}" 
            : $"FROM {_imageReference}";
        
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a WORKDIR statement in a Dockerfile.
/// </summary>
internal class DockerfileWorkDirStatement : DockerfileStatement
{
    private readonly string _path;

    public DockerfileWorkDirStatement(string path)
    {
        _path = path;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"WORKDIR {_path}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a RUN statement in a Dockerfile.
/// </summary>
internal class DockerfileRunStatement : DockerfileStatement
{
    private readonly string _command;

    public DockerfileRunStatement(string command)
    {
        _command = command;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"RUN {_command}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a COPY statement in a Dockerfile.
/// </summary>
internal class DockerfileCopyStatement : DockerfileStatement
{
    private readonly string _source;
    private readonly string _destination;

    public DockerfileCopyStatement(string source, string destination)
    {
        _source = source;
        _destination = destination;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"COPY {_source} {_destination}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a COPY --from statement in a Dockerfile.
/// </summary>
internal class DockerfileCopyFromStatement : DockerfileStatement
{
    private readonly string _stage;
    private readonly string _source;
    private readonly string _destination;

    public DockerfileCopyFromStatement(string stage, string source, string destination)
    {
        _stage = stage;
        _source = source;
        _destination = destination;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"COPY --from={_stage} {_source} {_destination}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents an ENV statement in a Dockerfile.
/// </summary>
internal class DockerfileEnvStatement : DockerfileStatement
{
    private readonly string _name;
    private readonly string _value;

    public DockerfileEnvStatement(string name, string value)
    {
        _name = name;
        _value = value;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"ENV {_name}={_value}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents an EXPOSE statement in a Dockerfile.
/// </summary>
internal class DockerfileExposeStatement : DockerfileStatement
{
    private readonly int _port;

    public DockerfileExposeStatement(int port)
    {
        _port = port;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var statement = $"EXPOSE {_port}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a CMD statement in a Dockerfile.
/// </summary>
internal class DockerfileCmdStatement : DockerfileStatement
{
    private readonly string[] _command;

    public DockerfileCmdStatement(string[] command)
    {
        _command = command;
    }

    public override async Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var commandJson = JsonSerializer.Serialize(_command, options);
        var statement = $"CMD {commandJson}";
        var bytes = Encoding.UTF8.GetBytes(statement + "\n");
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }
}