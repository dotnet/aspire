// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure OpenAI resources to the application model.
/// </summary>
public static class AzureOpenAIExtensions
{
    /// <summary>
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureOpenAIResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureOpenAIResource(name);
        return builder.AddResource(resource)
                      .WithParameter("name", resource.CreateBicepResourceName())
                      .WithParameter("deployments", resource.Deployments.Select(x => x.Name))
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment resource to the application model. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serverBuilder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the deployment.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIDeploymentResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> serverBuilder, string name)
    {
        var resource = new AzureOpenAIDeploymentResource(name, serverBuilder.Resource);
        return serverBuilder.ApplicationBuilder.AddResource(resource);
    }
}
