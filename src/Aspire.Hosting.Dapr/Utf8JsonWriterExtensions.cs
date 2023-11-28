// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.Dapr;

internal static class Uft8JsonWriterExtensions
{
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

    public static bool TryWriteBoolean(this Utf8JsonWriter writer, string name, bool? value)
    {
        if (value.HasValue)
        {
            writer.WriteBoolean(name, value.Value);

            return true;
        }

        return false;
    }

    public static bool TryWriteNumber(this Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);

            return true;
        }

        return false;
    }

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