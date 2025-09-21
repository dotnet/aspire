// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to enumerate files asynchronously.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}")]
public sealed class FilesCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilesCallbackAnnotation"/> class.
    /// </summary>
    /// <param name="callback">A callback that returns an async enumerable of file paths.</param>
    public FilesCallbackAnnotation(Func<CancellationToken, IAsyncEnumerable<string>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = callback;
    }

    /// <summary>
    /// Gets the callback that returns an async enumerable of file paths.
    /// </summary>
    public Func<CancellationToken, IAsyncEnumerable<string>> Callback { get; }
}