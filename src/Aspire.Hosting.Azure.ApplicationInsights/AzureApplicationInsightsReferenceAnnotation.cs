// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Initializes a new instance of the <see cref="AzureApplicationInsightsReferenceAnnotation"/> class.
/// </summary>
/// <param name="applicationInsightsResource"></param>
[Experimental("ASPIRECOMPUTE001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public class AzureApplicationInsightsReferenceAnnotation(AzureApplicationInsightsResource applicationInsightsResource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the Application Insights resource.
    /// </summary>
    public AzureApplicationInsightsResource ApplicationInsightsResource { get; } = applicationInsightsResource;
}
