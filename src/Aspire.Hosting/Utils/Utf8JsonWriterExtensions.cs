// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Extensions to the <see cref="Utf8JsonWriter"/> type.
/// </summary>
public static class Utf8JsonWriterExtensions
{
    /// <summary>
    /// Writes a string array to the JSON writer, if the array is not null or empty.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="name">The property name for the array.</param>
    /// <param name="values">The array values to write.</param>
    /// <returns>True if an array was written, otherwise false.</returns>
    public static bool TryWriteStringArray(this Utf8JsonWriter writer, string name, IEnumerable<string>? values)
    {
        if (values is not null)
        {
            var valuesList = values.ToList();

            if (valuesList.Any())
            {
                writer.WriteStartArray(name);

                foreach (var value in valuesList)
                {
                    writer.WriteStringValue(value);
                }

                writer.WriteEndArray();

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Writes a boolean value to the JSON writer, if the value is not null.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="name">The property name for the boolean.</param>
    /// <param name="value">The boolean value to write.</param>
    /// <returns>True if the value was written, otherwise, false.</returns>
    public static bool TryWriteBoolean(this Utf8JsonWriter writer, string name, bool? value)
    {
        if (value.HasValue)
        {
            writer.WriteBoolean(name, value.Value);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes a number to the JSON writer, if the value is not null.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="name">The property name for the number.</param>
    /// <param name="value">The number value to write.</param>
    /// <returns>True if the value was written, otherwise, false.</returns>
    public static bool TryWriteNumber(this Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes a string value to the JSON writer, if the value is not null.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="name">The property name for the string.</param>
    /// <param name="value">The string value to write.</param>
    /// <returns>True if the value was written, otherwise, false.</returns>
    public static bool TryWriteString(this Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null)
        {
            writer.WriteString(name, value);

            return true;
        }

        return false;
    }
}