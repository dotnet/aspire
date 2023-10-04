// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace Aspire.Dashboard.Otlp.Model;

public static class OtlpHelpers
{
    public static string? GetServiceId(this Resource resource)
    {
        string? serviceName = null;

        for (var i = 0; i < resource.Attributes.Count; i++)
        {
            var attribute = resource.Attributes[i];
            if (attribute.Key == OtlpApplication.SERVICE_INSTANCE_ID)
            {
                return attribute.Value.GetString();
            }
            if (attribute.Key == OtlpApplication.SERVICE_NAME)
            {
                serviceName = attribute.Value.GetString();
            }
        }

        //
        // NOTE: The service.instance.id value is a recommended attribute, but not required.
        //       See: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service-experimental
        //
        return serviceName;
    }

    public static string ToShortenedId(string id) =>
        id.Length > 7 ? id[..7] : id;

    public static string FormatTimeStamp(DateTime timestamp)
    {
        return timestamp.ToLocalTime().ToString("h:mm:ss.fff tt", CultureInfo.CurrentCulture);
    }

    public static string ToHexString(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        // This produces lowercase hex string from the bytes. It's used instead of Convert.ToHexString()
        // because we want to display lowercase hex string in the UI for values such as traceid and spanid.
        return string.Create(bytes.Length * 2, bytes, static (chars, bytes) =>
        {
            var data = bytes.Span;
            for (var pos = 0; pos < data.Length; pos++)
            {
                ToCharsBuffer(data[pos], chars, pos * 2);
            }
        });
    }

    public static string ToHexString(this ByteString bytes)
    {
        return ToHexString(bytes.Memory);
    }

    public static string GetString(this AnyValue value) =>
        value.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            AnyValue.ValueOneofCase.IntValue => value.IntValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.BoolValue => value.BoolValue ? "true" : "false",
            AnyValue.ValueOneofCase.BytesValue => value.BytesValue.ToHexString(),
            _ => value.ToString(),
        };

    // From https://github.com/dotnet/runtime/blob/963954a11c1beeea4ad63002084a213d8d742864/src/libraries/Common/src/System/HexConverter.cs#L81-L89
    // Modified slightly to always produce lowercase output.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0)
    {
        var difference = ((value & 0xF0U) << 4) + (value & 0x0FU) - 0x8989U;
        var packedResult = (((uint)-(int)difference & 0x7070U) >> 4) + difference + 0xB9B9U | 0x2020U;

        buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
        buffer[startingIndex] = (char)(packedResult >> 8);
    }

    public static DateTime UnixNanoSecondsToDateTime(ulong unixTimeNanoSeconds)
    {
        var ticks = NanoSecondsToTicks(unixTimeNanoSeconds);

        // Create a DateTime object for the Unix epoch (January 1, 1970)
        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        unixEpoch = unixEpoch.AddTicks(ticks);

        return unixEpoch;
    }

    private static long NanoSecondsToTicks(ulong nanoSeconds)
    {
        const ulong nanosecondsPerTick = 100; // 100 nanoseconds per tick
        return (long)(nanoSeconds / nanosecondsPerTick);
    }

    public static KeyValuePair<string, string>[] ToKeyValuePairs(this RepeatedField<KeyValue> attributes)
    {
        if (attributes.Count == 0)
        {
            return Array.Empty<KeyValuePair<string, string>>();
        }

        var values = new KeyValuePair<string, string>[attributes.Count];
        CopyKeyValues(attributes, values);

        return values;
    }

    public static void CopyKeyValuePairs(RepeatedField<KeyValue> attributes, [NotNull] ref KeyValuePair<string, string>[]? copiedAttributes)
    {
        if (copiedAttributes is null || copiedAttributes.Length < attributes.Count)
        {
            copiedAttributes = new KeyValuePair<string, string>[attributes.Count];
        }
        else
        {
            Array.Clear(copiedAttributes);
        }

        CopyKeyValues(attributes, copiedAttributes);
    }

    private static void CopyKeyValues(RepeatedField<KeyValue> attributes, KeyValuePair<string, string>[] copiedAttributes)
    {
        for (var i = 0; i < attributes.Count; i++)
        {
            var keyValue = attributes[i];
            copiedAttributes[i] = new KeyValuePair<string, string>(keyValue.Key, keyValue.Value.GetString());
        }
    }

    public static string? GetValue(this KeyValuePair<string, string>[] values, string name)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Key == name)
            {
                return values[i].Value;
            }
        }
        return null;
    }

    public static string ConcatProperties(this KeyValuePair<string, string>[] properties)
    {
        StringBuilder sb = new();
        var first = true;
        foreach (var kv in properties)
        {
            if (!first)
            {
                sb.Append(", ");
            }
            first = false;
            sb.Append(CultureInfo.InvariantCulture, $"{kv.Key}: ");
            sb.Append(string.IsNullOrEmpty(kv.Value) ? "\'\'" : kv.Value);
        }
        return sb.ToString();
    }

    public static PagedResult<T> GetItems<T>(IEnumerable<T> results, int startIndex, int? count)
    {
        return GetItems<T, T>(results, startIndex, count, null);
    }

    public static PagedResult<TResult> GetItems<TSource, TResult>(IEnumerable<TSource> results, int startIndex, int? count, Func<TSource, TResult>? select)
    {
        var query = results.Skip(startIndex);
        if (count != null)
        {
            query = query.Take(count.Value);
        }
        List<TResult> items;
        if (select != null)
        {
            items = query.Select(select).ToList();
        }
        else
        {
            items = query.Cast<TResult>().ToList();
        }
        var totalItemCount = results.Count();

        return new PagedResult<TResult>
        {
            Items = items,
            TotalItemCount = totalItemCount
        };
    }
}
