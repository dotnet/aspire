// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Application Insights resource.
/// </summary>
/// <param name="name">The resource name.</param>
public class AzureBicepApplicationInsightsResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.appinsights.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Application Insights resource.
    /// </summary>
    public string ConnectionStringExpression => $"{{{Name}.outputs.appInsightsConnectionString}}";

    /// <summary>
    /// Gets the connection string for the Azure Application Insights resource.
    /// </summary>
    /// <returns>The connection string for the Azure Application Insights resource.</returns>
    public string? GetConnectionString()
    {
        return Outputs["appInsightsConnectionString"];
    }

    // UseAzureMonitor is looks for this specific environment variable name.
    string IResourceWithConnectionString.ConnectionStringEnvironmentVariable => "APPLICATIONINSIGHTS_CONNECTION_STRING";
}

/// <summary>
/// Provides extension methods for adding the Azure ApplicationInsights resources to the application model.
/// </summary>
public static class AzureBicepApplicationInsightsExtensions
{
    /// <summary>
    /// Adds an Azure Application Insights resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureBicepApplicationInsightsResource> AddBicepApplicationInsights(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepApplicationInsightsResource(name);
        return builder.AddResource(resource)
                .WithParameter("appInsightsName", resource.CreateBicepResourceName())
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
