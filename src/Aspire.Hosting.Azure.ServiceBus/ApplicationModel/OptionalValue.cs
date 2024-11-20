// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus.ApplicationModel;

/// <summary>
/// Represents an optional value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed class OptionalValue<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalValue{T}"/> class.
    /// </summary>
    public OptionalValue()
    {
        Value = default;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionalValue{T}"/> class with a specified value.
    /// </summary>
    /// <param name="value">The value to initialize with.</param>
    public OptionalValue(T value)
    {
        Value = value;
        IsSet = true;
    }

    /// <summary>
    /// Gets or sets the literal value.
    /// </summary>
    public T? Value { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the value has been set.
    /// </summary>
    public bool IsSet { get; private set; }

    /// <summary>
    /// Assigns a value.
    /// </summary>
    /// <param name="lazyValue">The value to assign.</param>
    internal void Assign(OptionalValue<T> lazyValue)
    {
        Value = lazyValue.Value;
        IsSet = true;
    }

    /// <summary>
    /// Implicitly converts a value to a LazyValue{T}.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A LazyValue{T} containing the value.</returns>
    public static implicit operator OptionalValue<T>(T value)
    {
        return new(value);
    }

    /// <summary>
    /// Implicitly converts a value to a LazyValue{T}.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A LazyValue{T} containing the value.</returns>
    public static implicit operator T?(OptionalValue<T> value)
    {
       return value.Value;
    }
}
