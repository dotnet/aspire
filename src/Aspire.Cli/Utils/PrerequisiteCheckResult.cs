// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Utils;

/// <summary>
/// Represents the result of a prerequisite check.
/// </summary>
internal sealed class PrerequisiteCheckResult
{
    /// <summary>
    /// Gets the category of the check (e.g., "sdk", "container", "environment").
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the specific check (e.g., "dotnet-10", "daemon-running").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status of the check.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(LowercaseEnumConverter))]
    public PrerequisiteCheckStatus Status { get; init; }

    /// <summary>
    /// Gets the human-readable message describing the check result.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional fix suggestion for addressing the issue.
    /// </summary>
    [JsonPropertyName("fix")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fix { get; init; }

    /// <summary>
    /// Gets the optional documentation link for more information.
    /// </summary>
    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; init; }

    /// <summary>
    /// Gets optional additional details about the check.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Details { get; init; }
}

/// <summary>
/// Represents the status of a prerequisite check.
/// </summary>
internal enum PrerequisiteCheckStatus
{
    /// <summary>
    /// The check passed successfully.
    /// </summary>
    Pass,

    /// <summary>
    /// The check completed with a warning (non-blocking).
    /// </summary>
    Warning,

    /// <summary>
    /// The check failed (blocking issue).
    /// </summary>
    Fail
}

/// <summary>
/// JSON converter that serializes enums as lowercase strings.
/// </summary>
internal sealed class LowercaseEnumConverter : JsonConverter<PrerequisiteCheckStatus>
{
    public override PrerequisiteCheckStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "pass" => PrerequisiteCheckStatus.Pass,
            "warning" => PrerequisiteCheckStatus.Warning,
            "fail" => PrerequisiteCheckStatus.Fail,
            _ => throw new JsonException($"Unknown status value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PrerequisiteCheckStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}

/// <summary>
/// Represents the JSON output for the doctor command.
/// </summary>
internal sealed class DoctorCheckResponse
{
    /// <summary>
    /// Gets or sets the list of prerequisite check results.
    /// </summary>
    [JsonPropertyName("checks")]
    public required List<PrerequisiteCheckResult> Checks { get; set; }

    /// <summary>
    /// Gets or sets the summary of check results.
    /// </summary>
    [JsonPropertyName("summary")]
    public required DoctorCheckSummary Summary { get; set; }
}

/// <summary>
/// Represents the summary of doctor check results.
/// </summary>
internal sealed class DoctorCheckSummary
{
    /// <summary>
    /// Gets or sets the number of passed checks.
    /// </summary>
    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    /// <summary>
    /// Gets or sets the number of warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }

    /// <summary>
    /// Gets or sets the number of failed checks.
    /// </summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }
}
