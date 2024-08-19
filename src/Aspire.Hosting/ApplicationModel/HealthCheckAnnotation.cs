// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation that indicates the name of a health check associated with this resource.
/// </summary>
/// <param name="name">The name of the healthcheck registration.</param>
public class HealthCheckAnnotation(string name) : IResourceAnnotation
{
    /// <summary>
    /// The name of the health check registration.
    /// </summary>
    public string Name { get; } = name;
}
