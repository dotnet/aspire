// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure;

public class AzureBicepApplicationInsightsResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.appinsights.bicep"),
    IResourceWithConnectionString
{
    public string? GetConnectionString()
    {
        return Outputs["appInsightsConnectionString"];
    }
}

public static class AzureBicepApplicationInsightsExtensions
{
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
