// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a source annotation for a files resource.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Source = {Source}")]
public sealed class FilesSourceAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilesSourceAnnotation"/> class.
    /// </summary>
    /// <param name="source">The source path for the files.</param>
    public FilesSourceAnnotation(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
    }

    /// <summary>
    /// Gets the source path for the files.
    /// </summary>
    public string Source { get; }
}

/// <summary>
/// Marker annotation to indicate that the files resource initialization handler has been registered.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
internal sealed class FilesInitializationHandlerRegisteredAnnotation : IResourceAnnotation
{
}