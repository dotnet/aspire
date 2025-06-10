// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents a value that can be either a 32-bit integer or a string.
/// </summary>
/// <remarks>
/// This class provides functionality to handle values that could be either
/// an integer or a string. It supports implicit and explicit conversions,
/// equality comparisons, and YAML serialization/deserialization handling.
/// </remarks>
public sealed record Int32OrStringV1(int? Number = null, string? Text = null) : IEquatable<int>, IEquatable<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Int32OrStringV1"/> class with a 32-bit integer value.
    /// </summary>
    /// <param name="value">The integer value to initialize.</param>
    public Int32OrStringV1(int value) : this(Number: value)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Int32OrStringV1"/> class with a string value.
    /// </summary>
    /// <param name="value">The string value to initialize.</param>
    public Int32OrStringV1(string? value) : this(
        int.TryParse(value, out var intValue) ? intValue : null,
        !int.TryParse(value, out _) ? value : null)
    { }

    /// <summary>
    /// Gets the string value if the instance represents a string;
    /// otherwise, returns the string representation of the 32-bit integer value if the instance represents an integer.
    /// </summary>
    public string? Value =>
        Number?.ToString(CultureInfo.InvariantCulture) ?? Text;

    /// <summary>
    /// Determines whether the current instance is equal to another integer.
    /// </summary>
    /// <param name="other">The integer to compare with.</param>
    /// <returns>True if the current instance is equal to the other integer; otherwise, false.</returns>
    public bool Equals(int other) =>
        Number == other;

    /// <summary>
    /// Determines whether the current instance is equal to another string.
    /// </summary>
    /// <param name="other">The string to compare with.</param>
    /// <returns>True if the current instance is equal to the other string; otherwise, false.</returns>
    public bool Equals(string? other) =>
        Text == other;

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    /// <returns>The string representation of the value.</returns>
    public override string? ToString() => Value;

    /// <summary>
    /// Gets the value as a 32-bit integer.
    /// </summary>
    /// <param name="value">The value to get.</param>
    /// <returns></returns>
    /// <exception cref="InvalidCastException">Thrown if the value isn't a valid integer.</exception>
    public static explicit operator int(Int32OrStringV1 value) =>
        value.Number ?? throw new InvalidCastException("The specified value is not an Int32.");

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    /// <param name="value">The value to get.</param>
    /// <returns>The value as a string.</returns>
    public static explicit operator string?(Int32OrStringV1? value) =>
        value?.Text ?? value?.Number?.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the integer value as a Int32OrStringV1 instance.
    /// </summary>
    /// <param name="value">The integer to get.</param>
    /// <returns>An Int32OrStringV1 instance representing the integer value.</returns>
    public static implicit operator Int32OrStringV1(int value)
    {
        return new(value);
    }

    /// <summary>
    /// Gets the string value as a Int32OrStringV1 instance.
    /// </summary>
    /// <param name="value">The string to get</param>
    /// <returns>An Int32OrStringV1 instance representing the string value.</returns>
    public static implicit operator Int32OrStringV1?(string? value)
    {
        return value is not null ? new Int32OrStringV1(value) : null;
    }

    /// <summary>
    /// Compares an instance of Int32OrStringV1 to an integer for equality.
    /// </summary>
    /// <param name="left">An instance of Int32OrStringV1.</param>
    /// <param name="right">The integer instance to check against.</param>
    /// <returns>a boolean value indicating whether the two instances are equal.</returns>
    public static bool operator ==(Int32OrStringV1? left, int right)
    {
        return left is not null && left.Equals(right);
    }

    /// <summary>
    /// Compares an instance of Int32OrStringV1 to an integer for equality.
    /// </summary>
    /// <param name="left">An instance of Int32OrStringV1.</param>
    /// <param name="right">The integer instance to check against.</param>
    /// <returns>a boolean value indicating whether the two instances are not equal.</returns>
    public static bool operator !=(Int32OrStringV1? left, int right)
    {
        if (left is null)
        {
            return true;
        }

        return !left.Equals(right);
    }

    /// <summary>
    /// Compares an instance of Int32OrStringV1 to a string for equality.
    /// </summary>
    /// <param name="left">An instance of Int32OrStringV1.</param>
    /// <param name="right">The string instance to check against.</param>
    /// <returns>a boolean value indicating whether the two instances are equal.</returns>
    public static bool operator ==(Int32OrStringV1? left, string? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Compares an instance of Int32OrStringV1 to a string for equality.
    /// </summary>
    /// <param name="left">An instance of Int32OrStringV1.</param>
    /// <param name="right">The string instance to check against.</param>
    /// <returns>a boolean value indicating whether the two instances are not equal.</returns>
    public static bool operator !=(Int32OrStringV1? left, string? right)
    {
        if (left is null && right is null)
        {
            return false;
        }

        if (left is null || right is null)
        {
            return true;
        }

        return !left.Equals(right);
    }
}
