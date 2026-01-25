// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Tests.Helpers;

/// <summary>
/// Root configuration for test selection rules.
/// </summary>
public sealed class TestSelectionConfig
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// Glob patterns for files that should be completely ignored (no test triggers, no fallback).
    /// </summary>
    [JsonPropertyName("ignorePaths")]
    public List<string> IgnorePaths { get; set; } = [];

    /// <summary>
    /// Convention-based mappings for automatic test project discovery.
    /// </summary>
    [JsonPropertyName("projectMappings")]
    public List<ProjectMapping> ProjectMappings { get; set; } = [];

    /// <summary>
    /// Test categories with their trigger rules.
    /// </summary>
    [JsonPropertyName("categories")]
    public Dictionary<string, Category> Categories { get; set; } = [];

    /// <summary>
    /// Loads a TestSelectionConfig from a JSON file.
    /// </summary>
    public static TestSelectionConfig LoadFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<TestSelectionConfig>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize config from {filePath}");
    }

    /// <summary>
    /// Loads a TestSelectionConfig from a JSON string.
    /// </summary>
    public static TestSelectionConfig LoadFromJson(string json)
    {
        return JsonSerializer.Deserialize<TestSelectionConfig>(json)
            ?? throw new InvalidOperationException("Failed to deserialize config from JSON");
    }
}

/// <summary>
/// Maps source paths to test projects using pattern substitution.
/// </summary>
public sealed class ProjectMapping
{
    /// <summary>
    /// Glob pattern with {name} placeholder for source files.
    /// </summary>
    [JsonPropertyName("sourcePattern")]
    public string SourcePattern { get; set; } = "";

    /// <summary>
    /// Pattern with {name} placeholder for test project path.
    /// </summary>
    [JsonPropertyName("testPattern")]
    public string TestPattern { get; set; } = "";

    /// <summary>
    /// Glob patterns for paths to exclude from this mapping.
    /// </summary>
    [JsonPropertyName("exclude")]
    public List<string> Exclude { get; set; } = [];
}

/// <summary>
/// A test category configuration.
/// </summary>
public sealed class Category
{
    /// <summary>
    /// Human-readable description of the category.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// If any path matches, set run_all=true and run everything.
    /// </summary>
    [JsonPropertyName("triggerAll")]
    public bool TriggerAll { get; set; }

    /// <summary>
    /// Category runs whenever tests run (unless excluded).
    /// </summary>
    [JsonPropertyName("runByDefault")]
    public bool RunByDefault { get; set; }

    /// <summary>
    /// Glob patterns - if any match, category is triggered.
    /// </summary>
    [JsonPropertyName("triggerPaths")]
    public List<string> TriggerPaths { get; set; } = [];

    /// <summary>
    /// Glob patterns - matching files are excluded from triggering this category.
    /// </summary>
    [JsonPropertyName("excludePaths")]
    public List<string> ExcludePaths { get; set; } = [];

    /// <summary>
    /// Skip this category if changes are ONLY in listed categories.
    /// </summary>
    [JsonPropertyName("excludeWhenOnly")]
    public List<string> ExcludeWhenOnly { get; set; } = [];

    /// <summary>
    /// If this category triggers, also enable these categories.
    /// </summary>
    [JsonPropertyName("alsoTriggers")]
    public List<string> AlsoTriggers { get; set; } = [];

    /// <summary>
    /// Auto-map source paths to test projects via conventions.
    /// </summary>
    [JsonPropertyName("useConvention")]
    public bool UseConvention { get; set; }

    /// <summary>
    /// Explicit list of test projects for this category.
    /// </summary>
    [JsonPropertyName("projects")]
    public List<string> Projects { get; set; } = [];
}

/// <summary>
/// Result of test selection evaluation.
/// </summary>
public sealed class TestSelectionResult
{
    /// <summary>
    /// Whether to run all tests.
    /// </summary>
    public bool RunAll { get; set; }

    /// <summary>
    /// Reason for the test selection decision.
    /// </summary>
    public string TriggerReason { get; set; } = "";

    /// <summary>
    /// Category that triggered the run_all (if applicable).
    /// </summary>
    public string? TriggerCategory { get; set; }

    /// <summary>
    /// Pattern that triggered the run_all (if applicable).
    /// </summary>
    public string? TriggerPattern { get; set; }

    /// <summary>
    /// File that triggered the run_all (if applicable).
    /// </summary>
    public string? TriggerFile { get; set; }

    /// <summary>
    /// Files that caused conservative fallback (if applicable).
    /// </summary>
    public List<string> UnmatchedFiles { get; set; } = [];

    /// <summary>
    /// Active (non-ignored) changed files.
    /// </summary>
    public List<string> ChangedFiles { get; set; } = [];

    /// <summary>
    /// Ignored changed files.
    /// </summary>
    public List<string> IgnoredFiles { get; set; } = [];

    /// <summary>
    /// Per-category results.
    /// </summary>
    public Dictionary<string, CategoryResult> Categories { get; set; } = [];

    /// <summary>
    /// List of specific test projects to run.
    /// </summary>
    public List<string> Projects { get; set; } = [];

    /// <summary>
    /// Error message if evaluation failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Result for a single category.
/// </summary>
public sealed class CategoryResult
{
    /// <summary>
    /// Whether this category is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Reason for the category state.
    /// </summary>
    public string Reason { get; set; } = "";
}
