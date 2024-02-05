// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

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
    /// Gets the connection string for the Azure Application Insights resource.
    /// </summary>
    /// <returns>The connection string for the Azure Application Insights resource.</returns>
    public string? GetConnectionString()
    {
        return Outputs["appInsightsConnectionString"];
    }
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
        var resource = new AzureBicepApplicationInsightsResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.appInsightsConnectionString}}"
        };

        return builder.AddResource(resource)
                .WithParameter("appInsightsName", resource.CreateBicepResourceName())
                .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Injects a connection string as an environment variable from the source resource into the destination resource.
    /// The environment variable will be "APPLICATIONINSIGHTS_CONNECTION_STRING={connectionString}."
    /// <para>
    /// Each resource defines the format of the connection string value. The
    /// underlying connection string value can be retrieved using <see cref="IResourceWithConnectionString.GetConnectionString"/>.
    /// </para>
    /// <para>
    /// Connection strings are also resolved by the configuration system (appSettings.json in the AppHost project, or environment variables). If a connection string is not found on the resource, the configuration system will be queried for a connection string
    /// using the resource's name.
    /// </para>
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where connection string will be injected.</param>
    /// <param name="source">The Azure Application Insights resource from which to extract the connection string.</param>
    /// <param name="optional"><see langword="true"/> to allow a missing connection string; <see langword="false"/> to throw an exception if the connection string is not found.</param>
    /// <exception cref="DistributedApplicationException">Throws an exception if the connection string resolves to null. It can be null if the resource has no connection string, and if the configuration has no connection string for the source resource.</exception>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder,
        IResourceBuilder<AzureBicepApplicationInsightsResource> source, bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        var resource = source.Resource;

        return builder.WithEnvironment(context =>
        {
            // UseAzureMonitor is looking for this specific environment variable name.
            var connectionStringName = "APPLICATIONINSIGHTS_CONNECTION_STRING";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{resource.Name}.connectionString}}";
                return;
            }

            var connectionString = resource.GetConnectionString() ??
                builder.ApplicationBuilder.Configuration.GetConnectionString(resource.Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                if (optional)
                {
                    // This is an optional connection string, so we can just return.
                    return;
                }

                throw new DistributedApplicationException($"A connection string for '{resource.Name}' could not be retrieved.");
            }

            if (builder.Resource is ContainerResource)
            {
                connectionString = HostNameResolver.ReplaceLocalhostWithContainerHost(connectionString, builder.ApplicationBuilder.Configuration);
            }

            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }
}
