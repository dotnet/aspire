// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.ApplicationInsights;

/// <summary>
/// Annotation that indicates a resource is using a specific Azure Application Insights component.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AzureApplicationInsightsReferenceAnnotation"/> class.
/// </remarks>
/// <param name="applicationInsights">The application insights resource.</param>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class AzureApplicationInsightsReferenceAnnotation(AzureApplicationInsightsResource applicationInsights) : IResourceAnnotation
{
    /// <summary>
    /// Gets the application insights resource.
    /// </summary>
    public AzureApplicationInsightsResource ApplicationInsights { get; } = applicationInsights;
}
