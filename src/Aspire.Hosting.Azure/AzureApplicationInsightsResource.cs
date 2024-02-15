// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Azure Application Insights resource.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="connectionString">The connection string to use to connect.</param>
public sealed class AzureApplicationInsightsResource(string name, string? connectionString)
    : Resource(name), IAzureResource, IResourceWithConnectionString
{
    /// <summary>
    /// Gets or sets the connection string for the Azure Application Insights resource.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets the connection string for the Azure Application Insights resource.
    /// </summary>
    /// <returns>The connection string for the Azure Application Insights resource.</returns>
    public string? GetConnectionString() => ConnectionString;

    // UseAzureMonitor is looking for this specific environment variable name.
    string IResourceWithConnectionString.ConnectionStringEnvironmentVariable => "APPLICATIONINSIGHTS_CONNECTION_STRING";
}
