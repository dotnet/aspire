// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

// The purpose of this type is to improve the debugging experience when inspecting labels set without callback.
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {_name}, Value = {_value}")]
internal sealed class ContainerLabelAnnotation : ContainerLabelCallbackAnnotation
{
    private readonly string _name;
    private readonly string _value;

    public ContainerLabelAnnotation(string name, string value) : base(name, () => value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        _name = name;
        _value = value;
    }
}

/// <summary>
/// Represents an annotation that provides a callback to modify the container labels.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class ContainerLabelCallbackAnnotation : IResourceAnnotation
{
    private readonly string? _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelCallbackAnnotation"/> class with the specified name and callback function.
    /// </summary>
    /// <param name="name">The name of the container label to set.</param>
    /// <param name="callback">The callback function that returns the value to set the container label to.</param>
    public ContainerLabelCallbackAnnotation(string name, Func<string> callback)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);

        _name = name;
        Callback = (c) =>
        {
            c.Labels[name] = callback();
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelCallbackAnnotation"/> class with the specified callback action.
    /// </summary>
    /// <param name="callback">The callback action to be executed.</param>
    public ContainerLabelCallbackAnnotation(Action<Dictionary<string, string>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = (c) =>
        {
            callback(c.Labels);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public ContainerLabelCallbackAnnotation(Action<ContainerLabelCallbackContext> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = c =>
        {
            callback(c);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerLabelCallbackAnnotation"/> class with the specified callback.
    /// </summary>
    /// <param name="callback">The callback to be invoked.</param>
    public ContainerLabelCallbackAnnotation(Func<ContainerLabelCallbackContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = callback;
    }

    /// <summary>
    /// Gets or sets the callback action to be executed when the container labels are being built.
    /// </summary>
    public Func<ContainerLabelCallbackContext, Task> Callback { get; private set; }

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