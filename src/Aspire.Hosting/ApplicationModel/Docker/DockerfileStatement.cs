// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Represents a statement that can be written to a Dockerfile.
/// </summary>
[Experimental("ASPIREDOCKERFILEBUILDER001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public abstract class DockerfileStatement
{
    /// <summary>
    /// Writes the statement to the specified writer.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public abstract Task WriteStatementAsync(StreamWriter writer, CancellationToken cancellationToken = default);
}