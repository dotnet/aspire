// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Types;

[JsonDerivedType(typeof(AspireDashboardEvent), "aspire/dashboard")]
public partial class EventMessage {}

/// <summary>
/// Custom event sent by the Aspire debug adapter middleware to notify the IDE
/// about the Aspire dashboard URL and health status.
/// </summary>
public sealed class AspireDashboardEvent : EventMessage
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string? EventName => "aspire/dashboard";

    /// <summary>
    /// The event body containing dashboard information.
    /// </summary>
    [JsonPropertyName("body")]
    public new required AspireDashboardEventBody Body { get; set; }
}

/// <summary>
/// Body of the AspireDashboardEvent containing dashboard URLs and health status.
/// </summary>
public sealed class AspireDashboardEventBody
{
    /// <summary>
    /// The base URL of the Aspire dashboard including the login token query parameter.
    /// </summary>
    [JsonPropertyName("baseUrlWithLoginToken")]
    public string? BaseUrlWithLoginToken { get; set; }

    /// <summary>
    /// The Codespaces-specific URL of the Aspire dashboard including the login token,
    /// if running in GitHub Codespaces.
    /// </summary>
    [JsonPropertyName("codespacesUrlWithLoginToken")]
    public string? CodespacesUrlWithLoginToken { get; set; }

    /// <summary>
    /// Indicates whether the Aspire dashboard is healthy and accessible.
    /// </summary>
    [JsonPropertyName("dashboardHealthy")]
    public required bool DashboardHealthy { get; set; }
}
