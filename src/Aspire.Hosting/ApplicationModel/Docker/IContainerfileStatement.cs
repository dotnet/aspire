// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel.Docker;

/// <summary>
/// Represents a statement that can be written to a container file.
/// </summary>
public interface IContainerfileStatement
{
    /// <summary>
    /// Writes the statement to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteStatementAsync(Stream stream, CancellationToken cancellationToken = default);
}