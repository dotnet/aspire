// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.Utils;

public class FilterFieldValidatorTests
{
    private const string SampleFieldsJson = """
        {
          "traces": {
            "known_fields": [
              "trace.name",
              "trace.kind",
              "trace.status",
              "resource.servicename",
              "trace.traceid",
              "trace.spanid",
              "source.name"
            ],
            "custom_attributes": [
              "http.method",
              "http.url",
              "http.status_code",
              "db.system",
              "db.statement"
            ],
            "total_count": 12
          },
          "logs": {
            "known_fields": [
              "log.message",
              "log.category",
              "resource.servicename",
              "log.traceid",
              "log.spanid",
              "log.originalformat",
              "log.eventname"
            ],
            "custom_attributes": [
              "exception.type",
              "exception.message"
            ],
            "total_count": 9
          }
        }
        """;

    [Fact]
    public void ValidateFields_ValidKnownField_ReturnsEmptyList()
    {
        var fields = new[] { "trace.status" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateFields_ValidCustomAttribute_ReturnsEmptyList()
    {
        var fields = new[] { "http.method" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateFields_MultipleValidFields_ReturnsEmptyList()
    {
        var fields = new[] { "trace.status", "http.method", "trace.name" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateFields_UnknownField_ReturnsValidationError()
    {
        var fields = new[] { "unknown.field" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.Equal("unknown.field", results[0].FieldName);
        Assert.Contains("Unknown field", results[0].ErrorMessage);
    }

    [Fact]
    public void ValidateFields_SimilarField_ReturnsSuggestions()
    {
        // "trace.statu" is similar to "trace.status"
        var fields = new[] { "trace.statu" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.NotEmpty(results[0].Suggestions);
        Assert.Contains("trace.status", results[0].Suggestions);
    }

    [Fact]
    public void ValidateFields_TypoInField_ReturnsSuggestions()
    {
        // "http.metod" has a typo (missing 'h')
        var fields = new[] { "http.metod" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Single(results);
        Assert.False(results[0].IsValid);
        Assert.NotEmpty(results[0].Suggestions);
        Assert.Contains("http.method", results[0].Suggestions);
    }

    [Fact]
    public void ValidateFields_CaseInsensitive_ReturnsEmptyList()
    {
        var fields = new[] { "TRACE.STATUS", "HTTP.METHOD" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateFields_LogFields_ValidatesCorrectly()
    {
        var fields = new[] { "log.message", "exception.type" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "logs");

        Assert.Empty(results);
    }

    [Fact]
    public void ValidateFields_MixedValidAndInvalid_ReturnsOnlyInvalid()
    {
        var fields = new[] { "trace.status", "invalid.field", "http.method" };
        var results = FilterFieldValidator.ValidateFields(fields, SampleFieldsJson, "traces");

        Assert.Single(results);
        Assert.Equal("invalid.field", results[0].FieldName);
    }

    [Fact]
    public void ParseAvailableFields_ExtractsKnownFields()
    {
        var fields = FilterFieldValidator.ParseAvailableFields(SampleFieldsJson, "traces");

        Assert.Contains("trace.name", fields);
        Assert.Contains("trace.kind", fields);
        Assert.Contains("trace.status", fields);
    }

    [Fact]
    public void ParseAvailableFields_ExtractsCustomAttributes()
    {
        var fields = FilterFieldValidator.ParseAvailableFields(SampleFieldsJson, "traces");

        Assert.Contains("http.method", fields);
        Assert.Contains("http.url", fields);
        Assert.Contains("db.system", fields);
    }

    [Fact]
    public void ParseAvailableFields_WithMarkdownWrapper_ExtractsFields()
    {
        var jsonWithMarkdown = """
            # TELEMETRY FIELDS

            These fields can be used for filtering traces and logs.

            {
              "traces": {
                "known_fields": ["trace.name", "trace.status"],
                "custom_attributes": ["http.method"],
                "total_count": 3
              }
            }
            """;

        var fields = FilterFieldValidator.ParseAvailableFields(jsonWithMarkdown, "traces");

        Assert.Contains("trace.name", fields);
        Assert.Contains("trace.status", fields);
        Assert.Contains("http.method", fields);
    }

    [Fact]
    public void ParseAvailableFields_InvalidJson_ReturnsEmptySet()
    {
        var invalidJson = "not valid json";
        var fields = FilterFieldValidator.ParseAvailableFields(invalidJson, "traces");

        Assert.Empty(fields);
    }

    [Fact]
    public void GetSimilarFields_ExactPrefix_IncludesPrefixMatches()
    {
        var availableFields = new[] { "trace.name", "trace.status", "trace.kind", "http.method" };
        var suggestions = FilterFieldValidator.GetSimilarFields("trace", availableFields, maxSuggestions: 5);

        // Should include fields that start with "trace"
        Assert.True(suggestions.Count > 0);
    }

    [Fact]
    public void GetSimilarFields_NoSimilarFields_ReturnsEmptyList()
    {
        var availableFields = new[] { "trace.name", "trace.status" };
        var suggestions = FilterFieldValidator.GetSimilarFields("completely.different.field.name", availableFields, maxSuggestions: 3);

        Assert.Empty(suggestions);
    }

    [Fact]
    public void CalculateLevenshteinDistance_SameStrings_ReturnsZero()
    {
        var distance = FilterFieldValidator.CalculateLevenshteinDistance("test", "test");
        Assert.Equal(0, distance);
    }

    [Fact]
    public void CalculateLevenshteinDistance_SingleCharDifference_ReturnsOne()
    {
        var distance = FilterFieldValidator.CalculateLevenshteinDistance("test", "tost");
        Assert.Equal(1, distance);
    }

    [Fact]
    public void CalculateLevenshteinDistance_EmptyString_ReturnsOtherLength()
    {
        Assert.Equal(4, FilterFieldValidator.CalculateLevenshteinDistance("", "test"));
        Assert.Equal(4, FilterFieldValidator.CalculateLevenshteinDistance("test", ""));
    }

    [Fact]
    public void FormatValidationError_WithSuggestions_IncludesDidYouMean()
    {
        var result = new FilterFieldValidator.ValidationResult
        {
            IsValid = false,
            FieldName = "trace.statu",
            ErrorMessage = "Unknown field 'trace.statu'.",
            Suggestions = new[] { "trace.status", "trace.name" }
        };

        var message = FilterFieldValidator.FormatValidationError(result);

        Assert.Contains("Unknown field", message);
        Assert.Contains("Did you mean", message);
        Assert.Contains("trace.status", message);
    }

    [Fact]
    public void FormatValidationError_NoSuggestions_JustShowsError()
    {
        var result = new FilterFieldValidator.ValidationResult
        {
            IsValid = false,
            FieldName = "xyz.abc",
            ErrorMessage = "Unknown field 'xyz.abc'.",
            Suggestions = Array.Empty<string>()
        };

        var message = FilterFieldValidator.FormatValidationError(result);

        Assert.Contains("Unknown field", message);
        Assert.DoesNotContain("Did you mean", message);
    }
}
