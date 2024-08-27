// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation for an endpoint URL.
/// </summary>
/// <param name="endpointName">The name of the endpoint.</param>
/// <param name="relativeUrl">The URL relative to the endpoint.</param>
/// <param name="isLaunchUrl">Specifies whether the endpoint URL should launch at startup.</param>
/// <param name="excludeFromDashboard">Specifies whether the endpoint URL should be excluded from the dashboard.</param>
public sealed class EndpointUrlAnnotation(
    string endpointName,
    string relativeUrl,
    bool isLaunchUrl = false,
    bool excludeFromDashboard = false)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the name of the endpoint.
    /// </summary>
    public string EndpointName { get; } = endpointName;

    /// <summary>
    /// Gets the URL relative to the endpoint.
    /// </summary>
    public string RelativeUrl { get; } = relativeUrl;

    /// <summary>
    /// Gets a value indicating whether the endpoint URL should launch at startup.
    /// </summary>
    public bool IsLaunchUrl { get; } = isLaunchUrl;

    /// <summary>
    /// Gets a value indicating whether the endpoint URL should be excluded from the dashboard.
    /// </summary>
    public bool ExcludeFromDashboard { get; } = excludeFromDashboard;
}
