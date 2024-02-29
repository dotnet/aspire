// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to be executed with a list of command-line arguments when an executable resource is started.
/// </summary>
public class CommandLineArgsCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineArgsCallbackAnnotation"/> class with the specified callback action.
    /// </summary>
    /// <param name="callback"> The callback action to be executed.</param>
    public CommandLineArgsCallbackAnnotation(Func<CommandLineArgsCallbackContext, Task> callback)
    {
        Callback = callback;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineArgsCallbackAnnotation"/> class with the specified callback action.
    /// </summary>
    /// <param name="callback"> The callback action to be executed.</param>
    public CommandLineArgsCallbackAnnotation(Action<IList<string>> callback)
    {
        Callback = (c) =>
        {
            callback(c.Args);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Gets the callback action to be executed when the executable arguments are parsed.
    /// </summary>
    public Func<CommandLineArgsCallbackContext, Task> Callback { get; }
}

/// <summary>
/// Represents a callback context for the list of command-line arguments associated with an executable resource.
/// </summary>
/// <param name="args"> The list of command-line arguments.</param>
/// <param name="cancellationToken"> The cancellation token associated with this execution.</param>
public sealed class CommandLineArgsCallbackContext(IList<string> args, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets the list of command-line arguments.
    /// </summary>
    public IList<string> Args { get; } = args;

    /// <summary>
    /// Gets the cancellation token associated with the callback context.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;
}
