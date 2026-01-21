// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Model.Serialization;

/// <summary>
/// Represents a resource in JSON format.
/// </summary>
internal sealed class ResourceJson
{
    /// <summary>
    /// The name of the resource.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The type of the resource.
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; set; }

    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    /// <summary>
    /// The state of the resource.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// The creation timestamp of the resource.
    /// </summary>
    [JsonPropertyName("creationTimestamp")]
    public DateTime? CreationTimestamp { get; set; }

    /// <summary>
    /// The start timestamp of the resource.
    /// </summary>
    [JsonPropertyName("startTimestamp")]
    public DateTime? StartTimestamp { get; set; }

    /// <summary>
    /// The stop timestamp of the resource.
    /// </summary>
    [JsonPropertyName("stopTimestamp")]
    public DateTime? StopTimestamp { get; set; }

    /// <summary>
    /// The health status of the resource.
    /// </summary>
    [JsonPropertyName("healthStatus")]
    public string? HealthStatus { get; set; }

    /// <summary>
    /// The URLs associated with the resource.
    /// </summary>
    [JsonPropertyName("urls")]
    public ResourceUrlJson[]? Urls { get; set; }

    /// <summary>
    /// The volumes associated with the resource.
    /// </summary>
    [JsonPropertyName("volumes")]
    public ResourceVolumeJson[]? Volumes { get; set; }

    /// <summary>
    /// The environment variables associated with the resource.
    /// </summary>
    [JsonPropertyName("environment")]
    public ResourceEnvironmentVariableJson[]? Environment { get; set; }

    /// <summary>
    /// The health reports associated with the resource.
    /// </summary>
    [JsonPropertyName("healthReports")]
    public ResourceHealthReportJson[]? HealthReports { get; set; }

    /// <summary>
    /// The properties of the resource.
    /// </summary>
    [JsonPropertyName("properties")]
    public ResourcePropertyJson[]? Properties { get; set; }

    /// <summary>
    /// The relationships of the resource.
    /// </summary>
    [JsonPropertyName("relationships")]
    public ResourceRelationshipJson[]? Relationships { get; set; }
}

/// <summary>
/// Represents a URL in JSON format.
/// </summary>
internal sealed class ResourceUrlJson
{
    /// <summary>
    /// The name of the URL.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The display name of the URL.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// The URL value.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Represents a volume in JSON format.
/// </summary>
internal sealed class ResourceVolumeJson
{
    /// <summary>
    /// The source path of the volume.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// The target path of the volume.
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>
    /// The mount type of the volume.
    /// </summary>
    [JsonPropertyName("mountType")]
    public string? MountType { get; set; }

    /// <summary>
    /// Whether the volume is read-only.
    /// </summary>
    [JsonPropertyName("isReadOnly")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsReadOnly { get; set; }
}

/// <summary>
/// Represents an environment variable in JSON format.
/// </summary>
internal sealed class ResourceEnvironmentVariableJson
{
    /// <summary>
    /// The name of the environment variable.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The value of the environment variable.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Value { get; set; }
}

/// <summary>
/// Represents a health report in JSON format.
/// </summary>
internal sealed class ResourceHealthReportJson
{
    /// <summary>
    /// The name of the health report.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The health status.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// The description of the health report.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The exception message if any.
    /// </summary>
    [JsonPropertyName("exceptionMessage")]
    public string? ExceptionMessage { get; set; }
}

/// <summary>
/// Represents a property in JSON format.
/// </summary>
internal sealed class ResourcePropertyJson
{
    /// <summary>
    /// The name of the property.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The value of the property.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Value { get; set; }
}

/// <summary>
/// Represents a relationship in JSON format.
/// </summary>
internal sealed class ResourceRelationshipJson
{
    /// <summary>
    /// The type of the relationship.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The name of the related resource.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string? ResourceName { get; set; }
}
