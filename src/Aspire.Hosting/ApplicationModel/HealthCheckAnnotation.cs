// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation which tracks the name of the health check used to detect to health of a resource.
/// </summary>
/// <param name="key">The key for the health check in the app host which associated with this resource.</param>
public class HealthCheckAnnotation(string key) : IResourceAnnotation
{
    /// <summary>
    /// The key for the health check in the app host which associated with this resource.
    /// </summary>
    public string Key => key;
}
