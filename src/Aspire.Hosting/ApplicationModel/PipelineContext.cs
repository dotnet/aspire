// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides a shared context for passing data between pipeline steps.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PipelineContext
{
    private readonly ConcurrentDictionary<string, object> _data = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all outputs from the pipeline. Keys are typically in the format "StepName:OutputName".
    /// </summary>
    public IReadOnlyDictionary<string, object> Outputs => _data;

    /// <summary>
    /// Sets an output value for the current step.
    /// </summary>
    /// <param name="key">The key for the output, typically in the format "StepName:OutputName".</param>
    /// <param name="value">The value to store.</param>
    public void SetOutput(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _data[key] = value;
    }

    /// <summary>
    /// Tries to get an output value by key.
    /// </summary>
    /// <param name="key">The key for the output.</param>
    /// <param name="value">The output value if found.</param>
    /// <returns>True if the output was found, false otherwise.</returns>
    public bool TryGetOutput(string key, [NotNullWhen(true)] out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _data.TryGetValue(key, out value);
    }

    /// <summary>
    /// Tries to get an output value by key with a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the output value.</typeparam>
    /// <param name="key">The key for the output.</param>
    /// <param name="value">The output value if found and of the correct type.</param>
    /// <returns>True if the output was found and is of the correct type, false otherwise.</returns>
    public bool TryGetOutput<T>(string key, [NotNullWhen(true)] out T? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_data.TryGetValue(key, out var obj) && obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets an output value by key, throwing if not found.
    /// </summary>
    /// <param name="key">The key for the output.</param>
    /// <returns>The output value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found.</exception>
    public object GetOutput(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_data.TryGetValue(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"Output with key '{key}' was not found in the pipeline context.");
    }

    /// <summary>
    /// Gets an output value by key with a specific type, throwing if not found or wrong type.
    /// </summary>
    /// <typeparam name="T">The type of the output value.</typeparam>
    /// <param name="key">The key for the output.</param>
    /// <returns>The output value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value is not of the expected type.</exception>
    public T GetOutput<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_data.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Output with key '{key}' was not found in the pipeline context.");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException($"Output with key '{key}' is of type '{value.GetType().Name}' but expected type '{typeof(T).Name}'.");
    }

    /// <summary>
    /// Checks if an output with the specified key exists.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the output exists, false otherwise.</returns>
    public bool HasOutput(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _data.ContainsKey(key);
    }
}
