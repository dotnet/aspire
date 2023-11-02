// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to modify the environment variables of an application.
/// </summary>
public class EnvironmentCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified name and callback function.
    /// </summary>
    /// <param name="name">The name of the environment variable to set.</param>
    /// <param name="callback">The callback function that returns the value to set the environment variable to.</param>
    public EnvironmentCallbackAnnotation(string name, Func<string> callback)
    {
        Callback = (c) => c.EnvironmentVariables[name] = callback();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified callback action.
    /// </summary>
    /// <param name="callback">The callback action to be executed.</param>
    public EnvironmentCallbackAnnotation(Action<Dictionary<string, string>> callback)
    {
        Callback = (c) => callback(c.EnvironmentVariables);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public EnvironmentCallbackAnnotation(Action<EnvironmentCallbackContext> callback)
    {
        Callback = callback;
    }

    /// <summary>
    /// Gets or sets the callback action to be executed when the environment is being built.
    /// </summary>
    public Action<EnvironmentCallbackContext> Callback { get; private set; }
}
