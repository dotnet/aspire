// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Shared.Model.Serialization;

/// <summary>
/// Represents a resource in JSON format.
/// This is a shared representation used by both the Dashboard and CLI.
/// </summary>
internal sealed class ResourceJson
{
    /// <summary>
    /// The name of the resource.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The display name of the resource.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The type of the resource.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// The unique identifier of the resource.
    /// </summary>
    public string? Uid { get; set; }

    /// <summary>
    /// The state of the resource.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// The state style hint (e.g., "success", "error", "warning").
    /// </summary>
    public string? StateStyle { get; set; }

    /// <summary>
    /// The creation timestamp of the resource.
    /// </summary>
    public DateTimeOffset? CreationTimestamp { get; set; }

    /// <summary>
    /// The start timestamp of the resource.
    /// </summary>
    public DateTimeOffset? StartTimestamp { get; set; }

    /// <summary>
    /// The stop timestamp of the resource.
    /// </summary>
    public DateTimeOffset? StopTimestamp { get; set; }

    /// <summary>
    /// The exit code if the resource has exited.
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// The health status of the resource.
    /// </summary>
    public string? HealthStatus { get; set; }

    /// <summary>
    /// The URLs/endpoints associated with the resource.
    /// </summary>
    public ResourceUrlJson[]? Urls { get; set; }

    /// <summary>
    /// The volumes associated with the resource.
    /// </summary>
    public ResourceVolumeJson[]? Volumes { get; set; }

    /// <summary>
    /// The environment variables associated with the resource.
    /// </summary>
    public ResourceEnvironmentVariableJson[]? Environment { get; set; }

    /// <summary>
    /// The health reports associated with the resource.
    /// </summary>
    public ResourceHealthReportJson[]? HealthReports { get; set; }

    /// <summary>
    /// The properties of the resource.
    /// </summary>
    public ResourcePropertyJson[]? Properties { get; set; }

    /// <summary>
    /// The relationships of the resource.
    /// </summary>
    public ResourceRelationshipJson[]? Relationships { get; set; }
}

/// <summary>
/// Represents a URL/endpoint in JSON format.
/// </summary>
internal sealed class ResourceUrlJson
{
    /// <summary>
    /// The name of the URL/endpoint.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The display name of the URL.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The URL value.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Whether this is an internal endpoint.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsInternal { get; set; }
}

/// <summary>
/// Represents a volume in JSON format.
/// </summary>
internal sealed class ResourceVolumeJson
{
    /// <summary>
    /// The source path of the volume.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// The target path of the volume.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The mount type of the volume.
    /// </summary>
    public string? MountType { get; set; }

    /// <summary>
    /// Whether the volume is read-only.
    /// </summary>
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
    public string? Name { get; set; }

    /// <summary>
    /// The value of the environment variable.
    /// </summary>
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
    public string? Name { get; set; }

    /// <summary>
    /// The health status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The description of the health report.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The exception message if any.
    /// </summary>
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
    public string? Name { get; set; }

    /// <summary>
    /// The value of the property.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Value { get; set; }

    /// <summary>
    /// Whether this property contains sensitive data.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSensitive { get; set; }
}

/// <summary>
/// Represents a relationship in JSON format.
/// </summary>
internal sealed class ResourceRelationshipJson
{
    /// <summary>
    /// The type of the relationship.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The name of the related resource.
    /// </summary>
    public string? ResourceName { get; set; }
}
