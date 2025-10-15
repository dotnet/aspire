// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates

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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        var statement = _stageName is not null 
            ? $"FROM {_imageReference} AS {_stageName}" 
            : $"FROM {_imageReference}";
        
        await writer.WriteLineAsync(statement).ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"WORKDIR {_path}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"RUN {_command}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"COPY {_source} {_destination}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"COPY --from={_stage} {_source} {_destination}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"ENV {_name}={_value}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"EXPOSE {_port}").ConfigureAwait(false);
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

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var commandJson = JsonSerializer.Serialize(_command, options);
        await writer.WriteLineAsync($"CMD {commandJson}").ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a USER statement in a Dockerfile.
/// </summary>
internal class DockerfileUserStatement : DockerfileStatement
{
    private readonly string _user;

    public DockerfileUserStatement(string user)
    {
        _user = user;
    }

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        await writer.WriteLineAsync($"USER {_user}").ConfigureAwait(false);
    }
}

/// <summary>
/// Represents a comment in a Dockerfile.
/// </summary>
internal class DockerfileCommentStatement : DockerfileStatement
{
    private readonly string _comment;

    public DockerfileCommentStatement(string comment)
    {
        _comment = comment;
    }

    public override async Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default)
    {
        // Split by newlines to handle multi-line comments
        var lines = _comment.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        foreach (var line in lines)
        {
            await writer.WriteLineAsync($"# {line}").ConfigureAwait(false);
        }
    }
}