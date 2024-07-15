// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that holds a filter that determines if environment variables should be injected for a given endpoint.
/// </summary>
internal class EndpointEnvironmentInjectionFilterAnnotation(Func<EndpointAnnotation, bool> filter) : IResourceAnnotation
{
    public Func<EndpointAnnotation, bool> Filter { get; } = filter;
}
