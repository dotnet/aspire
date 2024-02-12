// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace Aspire.Dashboard.Otlp.Model;

public static partial class OtlpHelpers
{
    private static readonly string s_longTimePatternWithMilliseconds = GetLongTimePatternWithMilliseconds();

    static string GetLongTimePatternWithMilliseconds()
    {
        // From https://learn.microsoft.com/dotnet/standard/base-types/how-to-display-milliseconds-in-date-and-time-values

        // Gets the long time pattern, which is something like "h:mm:ss tt" (en-US), "H:mm:ss" (ja-JP), "HH:mm:ss" (fr-FR).
        var longTimePattern = DateTimeFormatInfo.CurrentInfo.LongTimePattern;

        // Create a format similar to .fff but based on the current culture.
        var millisecondFormat = $"{NumberFormatInfo.CurrentInfo.NumberDecimalSeparator}fff";

        // Append millisecond pattern to current culture's long time pattern.
        return MatchSecondsInTimeFormatPattern().Replace(longTimePattern, $"$1{millisecondFormat}");
    }

    [GeneratedRegex("(:ss|:s)")]
    private static partial Regex MatchSecondsInTimeFormatPattern();

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
        return timestamp.ToLocalTime().ToString(s_longTimePatternWithMilliseconds, CultureInfo.CurrentCulture);
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

    public static DateTime UnixNanoSecondsToDateTime(ulong unixTimeNanoseconds)
    {
        var ticks = NanosecondsToTicks(unixTimeNanoseconds);

        return DateTime.UnixEpoch.AddTicks(ticks);
    }

    private static long NanosecondsToTicks(ulong nanoseconds)
    {
        return (long)(nanoseconds / TimeSpan.NanosecondsPerTick);
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

    public static string? GetPeerAddress(this KeyValuePair<string, string>[] values)
    {
        var address = GetValue(values, OtlpSpan.PeerServiceAttributeKey);
        if (address != null)
        {
            return address;
        }

        // OTEL HTTP 1.7.0 doesn't return peer.service. Fallback to server.address and server.port.
        if (GetValue(values, OtlpSpan.ServerAddressAttributeKey) is { } server)
        {
            if (GetValue(values, OtlpSpan.ServerPortAttributeKey) is { } serverPort)
            {
                server += ":" + serverPort;
            }
            return server;
        }

        // Fallback to older names of net.peer.name and net.peer.port.
        if (GetValue(values, OtlpSpan.NetPeerNameAttributeKey) is { } peer)
        {
            if (GetValue(values, OtlpSpan.NetPeerPortAttributeKey) is { } peerPort)
            {
                peer += ":" + peerPort;
            }
            return peer;
        }

        return null;
    }

    public static bool HasKey(this KeyValuePair<string, string>[] values, string name)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i].Key == name)
            {
                return true;
            }
        }
        return false;
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
