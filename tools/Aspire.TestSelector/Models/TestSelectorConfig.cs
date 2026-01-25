// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.TestSelector.Models;

/// <summary>
/// Root configuration for the MSBuild-based test selector.
/// </summary>
public sealed class TestSelectorConfig
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// Glob patterns for files that should be completely ignored (no test triggers).
    /// </summary>
    [JsonPropertyName("ignorePaths")]
    public List<string> IgnorePaths { get; set; } = [];

    /// <summary>
    /// Glob patterns for files that trigger ALL tests (critical build infrastructure).
    /// </summary>
    [JsonPropertyName("triggerAllPaths")]
    public List<string> TriggerAllPaths { get; set; } = [];

    /// <summary>
    /// Glob patterns to exclude from triggerAllPaths (e.g., pipeline YAML files).
    /// </summary>
    [JsonPropertyName("triggerAllExclude")]
    public List<string> TriggerAllExclude { get; set; } = [];

    /// <summary>
    /// Rules for non-.NET files that map to specific test categories.
    /// </summary>
    [JsonPropertyName("nonDotNetRules")]
    public List<NonDotNetRule> NonDotNetRules { get; set; } = [];

    /// <summary>
    /// Test category configurations.
    /// </summary>
    [JsonPropertyName("categories")]
    public Dictionary<string, CategoryConfig> Categories { get; set; } = [];

    /// <summary>
    /// Loads a TestSelectorConfig from a JSON file.
    /// </summary>
    public static TestSelectorConfig LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

    /// <summary>
    /// Loads a TestSelectorConfig from a JSON string.
    /// </summary>
    public static TestSelectorConfig LoadFromJson(string json)
    {
        return JsonSerializer.Deserialize<TestSelectorConfig>(json, s_jsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize config from JSON");
    }
}

/// <summary>
/// A rule for mapping non-.NET file patterns to test categories.
/// </summary>
public sealed class NonDotNetRule
{
    /// <summary>
    /// Glob pattern for matching files.
    /// </summary>
    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";

    /// <summary>
    /// The test category to trigger when the pattern matches.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";
}

/// <summary>
/// Configuration for a test category.
/// </summary>
public sealed class CategoryConfig
{
    /// <summary>
    /// Human-readable description of the category.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Explicit list of test project paths for this category.
    /// Use "auto" to derive from MSBuild (for integrations category).
    /// </summary>
    [JsonPropertyName("testProjects")]
    [JsonConverter(typeof(TestProjectsConverter))]
    public TestProjectsValue TestProjects { get; set; } = new();
}

/// <summary>
/// Represents the testProjects value which can be either a list of strings or "auto".
/// </summary>
public sealed class TestProjectsValue
{
    public bool IsAuto { get; set; }
    public List<string> Projects { get; set; } = [];
}

/// <summary>
/// JSON converter for TestProjectsValue that handles both string[] and "auto".
/// </summary>
public sealed class TestProjectsConverter : JsonConverter<TestProjectsValue>
{
    public override TestProjectsValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new TestProjectsValue();

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (value?.Equals("auto", StringComparison.OrdinalIgnoreCase) == true)
            {
                result.IsAuto = true;
            }
            return result;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var project = reader.GetString();
                    if (!string.IsNullOrEmpty(project))
                    {
                        result.Projects.Add(project);
                    }
                }
            }
            return result;
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, TestProjectsValue value, JsonSerializerOptions options)
    {
        if (value.IsAuto)
        {
            writer.WriteStringValue("auto");
        }
        else
        {
            writer.WriteStartArray();
            foreach (var project in value.Projects)
            {
                writer.WriteStringValue(project);
            }
            writer.WriteEndArray();
        }
    }
}
