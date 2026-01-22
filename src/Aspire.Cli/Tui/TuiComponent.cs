// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Rendering;

namespace Aspire.Cli.Tui;

/// <summary>
/// Base class for TUI components with state management and lifecycle.
/// </summary>
internal abstract class TuiComponent
{
    private readonly Dictionary<string, object?> _state = [];
    private readonly Dictionary<string, object?> _props;

    /// <summary>
    /// Gets or sets the runtime that manages this component.
    /// </summary>
    public TuiRuntime? Runtime { get; set; }

    protected TuiComponent(Dictionary<string, object?>? props = null)
    {
        _props = props ?? [];
    }

    /// <summary>
    /// Called when the component is first mounted.
    /// </summary>
    public virtual void ComponentDidMount() { }

    /// <summary>
    /// Called when the component is being unmounted.
    /// </summary>
    public virtual void ComponentWillUnmount() { }

    /// <summary>
    /// Renders the component.
    /// </summary>
    public abstract IRenderable Render();

    /// <summary>
    /// Sets a state value and triggers a rerender.
    /// </summary>
    protected void SetState(string key, object? value)
    {
        _state[key] = value;
        ScheduleRerender();
    }

    /// <summary>
    /// Gets a state value.
    /// </summary>
    protected T? GetState<T>(string key, T? defaultValue = default)
    {
        return _state.TryGetValue(key, out var value) ? (T?)value : defaultValue;
    }

    /// <summary>
    /// Gets a prop value.
    /// </summary>
    protected T? GetProp<T>(string key, T? defaultValue = default)
    {
        return _props.TryGetValue(key, out var value) ? (T?)value : defaultValue;
    }

    /// <summary>
    /// Schedules a rerender of the UI.
    /// </summary>
    protected void ScheduleRerender()
    {
        Runtime?.ScheduleRerender();
    }
}

/// <summary>
/// Interface for components that handle keyboard input.
/// </summary>
internal interface IInputHandler
{
    /// <summary>
    /// Handles a key press.
    /// </summary>
    /// <param name="keyInfo">The key that was pressed.</param>
    /// <returns>True if the input was handled; otherwise, false.</returns>
    bool HandleInput(ConsoleKeyInfo keyInfo);
}
