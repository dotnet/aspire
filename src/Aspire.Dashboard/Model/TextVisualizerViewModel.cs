// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Model;

public class TextVisualizerViewModel
{
    private readonly string _text;

    public string FormatKind { get; private set; } = default!;
    public string FormattedText { get; private set; } = default!;
    public List<StringLogLine> FormattedLines { get; set; } = [];

    public TextVisualizerViewModel(string text)
    {
        _text = text;

        if (TryFormatJson(_text, out var formattedJson))
        {
            ChangeFormattedText(DashboardUIHelpers.JsonFormat, formattedJson);
        }
        else if (TryFormatXml(_text, out var formattedXml))
        {
            ChangeFormattedText(DashboardUIHelpers.XmlFormat, formattedXml);
        }
        else
        {
            ChangeFormattedText(DashboardUIHelpers.PlaintextFormat, _text);
        }
    }

    private static bool TryFormatXml(string text, [NotNullWhen(true)] out string? formattedText)
    {
        try
        {
            var document = XDocument.Parse(text);
            var stringWriter = new StringWriter();
            document.Save(stringWriter);
            formattedText = stringWriter.ToString();
            return true;
        }
        catch (XmlException)
        {
            formattedText = null;
            return false;
        }
    }

    private void ChangeFormattedText(string format, string formattedText)
    {
        FormatKind = format;
        FormattedText = formattedText;
        FormattedLines = GetLines(FormattedText, FormatKind);
    }

    private static List<StringLogLine> GetLines(string formattedText, string formatKind)
    {
        var lines = formattedText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();

        return lines.Select((line, index) => new StringLogLine(index + 1, line, formatKind != DashboardUIHelpers.PlaintextFormat)).ToList();
    }

    private static bool TryFormatJson(string text, [NotNullWhen(true)] out string? formattedText)
    {
        try
        {
            formattedText = FormatJson(text);
            return true;
        }
        catch (JsonException)
        {
            formattedText = null;
            return false;
        }
    }

    private static string FormatJson(string jsonString)
    {
        var jsonData = Encoding.UTF8.GetBytes(jsonString);

        // Initialize the Utf8JsonReader
        var reader = new Utf8JsonReader(jsonData, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Allow,
            // Increase the allowed limit to 1000. This matches the allowed limit of the writer.
            // It's ok to allow recursion here because JSON is read in a flat loop. There isn't a danger
            // of recursive method calls that cause a stack overflow.
            MaxDepth = 1000
        });

        // Use a MemoryStream and Utf8JsonWriter to write the formatted JSON
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    break;
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString()!);
                    break;
                case JsonTokenType.String:
                    writer.WriteStringValue(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out var intValue))
                    {
                        writer.WriteNumberValue(intValue);
                    }
                    else if (reader.TryGetDouble(out var doubleValue))
                    {
                        writer.WriteNumberValue(doubleValue);
                    }
                    break;
                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;
                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
                case JsonTokenType.Comment:
                    writer.WriteCommentValue(reader.GetComment());
                    break;
            }
        }

        writer.Flush();
        var formattedJson = Encoding.UTF8.GetString(stream.ToArray());

        return formattedJson;
    }

    internal void UpdateFormat(string newFormat)
    {
        if (newFormat == DashboardUIHelpers.XmlFormat)
        {
            if (TryFormatXml(_text, out var formattedXml))
            {
                ChangeFormattedText(newFormat, formattedXml);
            }
        }
        else if (newFormat == DashboardUIHelpers.JsonFormat)
        {
            if (TryFormatJson(_text, out var formattedJson))
            {
                ChangeFormattedText(newFormat, formattedJson);
            }
        }
        else
        {
            ChangeFormattedText(DashboardUIHelpers.PlaintextFormat, _text);
        }
    }
}
