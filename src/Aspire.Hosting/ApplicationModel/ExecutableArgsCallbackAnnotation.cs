// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to be executed with a list of command-line arguments when an executable resource is started.
/// </summary>
/// <param name="callback">The callback action to be executed.</param>
public class ExecutableArgsCallbackAnnotation(Action<IList<string>> callback) : IResourceAnnotation
{
    /// <summary>
    /// Gets the callback action to be executed when the executable arguments are parsed.
    /// </summary>
    public Action<IList<string>> Callback { get; } = callback;
}
