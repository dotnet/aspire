// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// A string value that can optionally carry culture-specific translations.
/// Deserializes from either a plain JSON string or an object with culture keys.
/// </summary>
[JsonConverter(typeof(LocalizableStringJsonConverter))]
internal sealed class LocalizableString
{
    private readonly string? _value;
    private readonly Dictionary<string, string>? _localizations;

    private LocalizableString(string value)
    {
        _value = value;
    }

    private LocalizableString(Dictionary<string, string> localizations)
    {
        _localizations = localizations;
    }

    /// <summary>
    /// Creates a <see cref="LocalizableString"/> from a plain string.
    /// </summary>
    public static LocalizableString FromString(string value) => new(value);

    /// <summary>
    /// Creates a <see cref="LocalizableString"/> from culture-keyed translations.
    /// </summary>
    public static LocalizableString FromLocalizations(Dictionary<string, string> localizations) => new(localizations);

    /// <summary>
    /// Resolves the best string for the current UI culture.
    /// </summary>
    public string Resolve()
    {
        if (_value is not null)
        {
            return _value;
        }

        if (_localizations is null or { Count: 0 })
        {
            return string.Empty;
        }

        var culture = CultureInfo.CurrentUICulture;

        // Try exact match (e.g., "en-US").
        if (_localizations.TryGetValue(culture.Name, out var exact))
        {
            return exact;
        }

        // Try parent culture (e.g., "en").
        if (!string.IsNullOrEmpty(culture.Parent?.Name) &&
            _localizations.TryGetValue(culture.Parent.Name, out var parent))
        {
            return parent;
        }

        // Fall back to first entry.
        foreach (var kvp in _localizations)
        {
            return kvp.Value;
        }

        return string.Empty;
    }

    public override string ToString() => Resolve();

    public static implicit operator LocalizableString(string value) => FromString(value);
}

/// <summary>
/// Handles deserializing a <see cref="LocalizableString"/> from a JSON string or object.
/// </summary>
internal sealed class LocalizableStringJsonConverter : JsonConverter<LocalizableString>
{
    public override LocalizableString? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return LocalizableString.FromString(reader.GetString() ?? string.Empty);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var localizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in localizable string object.");
                }

                var key = reader.GetString()!;
                reader.Read();
                var value = reader.GetString() ?? string.Empty;
                localizations[key] = value;
            }

            return LocalizableString.FromLocalizations(localizations);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for LocalizableString.");
    }

    public override void Write(Utf8JsonWriter writer, LocalizableString value, JsonSerializerOptions options)
    {
        // Serialize as a plain string for simplicity.
        writer.WriteStringValue(value.Resolve());
    }
}
