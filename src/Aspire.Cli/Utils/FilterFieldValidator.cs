// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Cli.Utils;

/// <summary>
/// Validates filter field names against available telemetry fields and provides suggestions for similar field names.
/// </summary>
internal static class FilterFieldValidator
{
    /// <summary>
    /// Result of validating a filter field.
    /// </summary>
    internal sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public string? FieldName { get; init; }
        public string? ErrorMessage { get; init; }
        public IReadOnlyList<string> Suggestions { get; init; } = [];
    }

    /// <summary>
    /// Validates the given filter fields against the available fields from the telemetry repository.
    /// </summary>
    /// <param name="filterFields">The filter field names to validate.</param>
    /// <param name="fieldsJson">The JSON response from list_telemetry_fields tool containing available fields.</param>
    /// <param name="telemetryType">The type of telemetry ("traces" or "logs") to validate against.</param>
    /// <returns>A list of validation results, one for each invalid field. Returns empty list if all fields are valid.</returns>
    public static IReadOnlyList<ValidationResult> ValidateFields(
        IEnumerable<string> filterFields,
        string fieldsJson,
        string telemetryType)
    {
        var availableFields = ParseAvailableFields(fieldsJson, telemetryType);
        var results = new List<ValidationResult>();

        foreach (var field in filterFields)
        {
            if (!availableFields.Contains(field, StringComparer.OrdinalIgnoreCase))
            {
                var suggestions = GetSimilarFields(field, availableFields, maxSuggestions: 3);
                results.Add(new ValidationResult
                {
                    IsValid = false,
                    FieldName = field,
                    ErrorMessage = $"Unknown field '{field}'.",
                    Suggestions = suggestions
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Parses the available fields from the list_telemetry_fields JSON response.
    /// </summary>
    internal static HashSet<string> ParseAvailableFields(string fieldsJson, string telemetryType)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Extract JSON from potential markdown wrapper
            var jsonContent = ExtractJsonFromResponse(fieldsJson);
            if (string.IsNullOrEmpty(jsonContent))
            {
                return fields;
            }

            using var document = JsonDocument.Parse(jsonContent);
            var root = document.RootElement;

            // Try to get the specific telemetry type section
            var sectionName = telemetryType.ToLowerInvariant();
            if (root.TryGetProperty(sectionName, out var typeSection))
            {
                AddFieldsFromSection(typeSection, fields);
            }
            else
            {
                // If no specific section, try both traces and logs
                if (root.TryGetProperty("traces", out var tracesSection))
                {
                    AddFieldsFromSection(tracesSection, fields);
                }
                if (root.TryGetProperty("logs", out var logsSection))
                {
                    AddFieldsFromSection(logsSection, fields);
                }
            }
        }
        catch (JsonException)
        {
            // If parsing fails, return empty set - validation will be skipped
        }

        return fields;
    }

    private static void AddFieldsFromSection(JsonElement section, HashSet<string> fields)
    {
        // Add known fields
        if (section.TryGetProperty("known_fields", out var knownFields) && knownFields.ValueKind == JsonValueKind.Array)
        {
            foreach (var field in knownFields.EnumerateArray())
            {
                if (field.ValueKind == JsonValueKind.String)
                {
                    fields.Add(field.GetString()!);
                }
            }
        }

        // Add custom attributes
        if (section.TryGetProperty("custom_attributes", out var customAttrs) && customAttrs.ValueKind == JsonValueKind.Array)
        {
            foreach (var attr in customAttrs.EnumerateArray())
            {
                if (attr.ValueKind == JsonValueKind.String)
                {
                    fields.Add(attr.GetString()!);
                }
            }
        }
    }

    private static string ExtractJsonFromResponse(string response)
    {
        // The MCP tool response may contain markdown headers followed by JSON
        var lines = response.Split('\n');
        var jsonStartIndex = -1;

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            {
                jsonStartIndex = i;
                break;
            }
        }

        if (jsonStartIndex >= 0)
        {
            return string.Join('\n', lines.Skip(jsonStartIndex));
        }

        return response;
    }

    /// <summary>
    /// Finds fields similar to the given field name using Levenshtein distance.
    /// </summary>
    internal static List<string> GetSimilarFields(string field, IEnumerable<string> availableFields, int maxSuggestions = 3)
    {
        var fieldLower = field.ToLowerInvariant();
        var suggestions = availableFields
            .Select(f => new { Field = f, Distance = CalculateLevenshteinDistance(fieldLower, f.ToLowerInvariant()) })
            .Where(x => x.Distance <= Math.Max(3, field.Length / 2)) // Only suggest if reasonably similar
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Field) // Alphabetical for same distance
            .Take(maxSuggestions)
            .Select(x => x.Field)
            .ToList();

        // Also add prefix matches if not already included
        var prefixMatches = availableFields
            .Where(f => f.StartsWith(field, StringComparison.OrdinalIgnoreCase) ||
                       field.StartsWith(f, StringComparison.OrdinalIgnoreCase))
            .Where(f => !suggestions.Contains(f, StringComparer.OrdinalIgnoreCase))
            .Take(maxSuggestions - suggestions.Count);

        suggestions.AddRange(prefixMatches);

        return suggestions.Take(maxSuggestions).ToList();
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// </summary>
    internal static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Create distance matrix
        var distance = new int[sourceLength + 1, targetLength + 1];

        // Initialize first column
        for (var i = 0; i <= sourceLength; i++)
        {
            distance[i, 0] = i;
        }

        // Initialize first row
        for (var j = 0; j <= targetLength; j++)
        {
            distance[0, j] = j;
        }

        // Fill in the rest of the matrix
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(
                        distance[i - 1, j] + 1,      // Deletion
                        distance[i, j - 1] + 1),    // Insertion
                    distance[i - 1, j - 1] + cost); // Substitution
            }
        }

        return distance[sourceLength, targetLength];
    }

    /// <summary>
    /// Formats a validation error message with suggestions.
    /// </summary>
    public static string FormatValidationError(ValidationResult result)
    {
        var message = result.ErrorMessage ?? $"Unknown field '{result.FieldName}'.";

        if (result.Suggestions.Count > 0)
        {
            message += $" Did you mean: {string.Join(", ", result.Suggestions.Select(s => $"'{s}'"))}?";
        }

        return message;
    }
}
