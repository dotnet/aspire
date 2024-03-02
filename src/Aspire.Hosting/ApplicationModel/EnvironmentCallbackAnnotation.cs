// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to modify the environment variables of an application.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class EnvironmentCallbackAnnotation : IResourceAnnotation
{
    private readonly string? _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified name and callback function.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="callback">The callback function that returns the value to set the environment variable to.</param>
    public EnvironmentCallbackAnnotation(string name, Func<string> callback)
    {
        _name = name;
        Callback = (c) =>
        {
            c.EnvironmentVariables[name] = callback();
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified callback action.
    /// </summary>
    /// <param name="callback">The callback action to be executed.</param>
    public EnvironmentCallbackAnnotation(Action<Dictionary<string, string>> callback)
    {
        Callback = (c) =>
        {
            callback(c.EnvironmentVariables);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public EnvironmentCallbackAnnotation(Action<EnvironmentCallbackContext> callback)
    {
        Callback = c =>
        {
            callback(c);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public EnvironmentCallbackAnnotation(Func<EnvironmentCallbackContext, Task> callback)
    {
        Callback = callback;
    }

    /// <summary>
    /// Gets or sets the callback action to be executed when the environment is being built.
    /// </summary>
    public Func<EnvironmentCallbackContext, Task> Callback { get; private set; }

    private string DebuggerToString()
    {
        var text = $@"Type = {GetType().Name}";
        if (_name != null)
        {
            text += $@", Name = ""{_name}""";
        }
        return text;
    }
}
