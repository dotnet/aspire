// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Bicep;
using Aspire.Hosting.Azure.Postgres;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Postgres resources to the application model.
/// </summary>
public static class AzurePostgresExtensions
{
    ///// <summary>
    ///// Adds an Azure Postgres resource to the application model. This resource can be used to create Azure Postgres Flexible Server resources.
    ///// </summary>
    ///// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    ///// <param name="name">The name of the resource.</param>
    ///// <param name="administratorLogin">The administrator login.</param>
    ///// <param name="administratorLoginPassword">The administrator password.</param>
    ///// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    //public static IResourceBuilder<AzureBicepPostgresResource> AddBicepAzurePostgres(this IDistributedApplicationBuilder builder,
    //    string name,
    //    string administratorLogin,
    //    IResourceBuilder<ParameterResource> administratorLoginPassword)
    //{
    //    var resource = new AzureBicepPostgresResource(name);

    //    return builder.AddResource(resource)
    //        .WithParameter("serverName", resource.CreateBicepResourceName())
    //        .WithParameter("administratorLogin", administratorLogin)
    //        .WithParameter("administratorLoginPassword", administratorLoginPassword)
    //        .WithParameter("databases", resource.Databases)
    //        .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
    //        .WithManifestPublishingCallback(resource.WriteToManifest);
    //}

    /// <summary>
    /// TODO: doc comments
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="administratorLogin"></param>
    /// <param name="administratorLoginPassword"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, IResourceBuilder<ParameterResource> administratorLogin, IResourceBuilder<ParameterResource> administratorLoginPassword, Action<IResourceBuilder<AzurePostgresResource>>? callback = null)
    {
        var resource = new AzurePostgresResource(builder.Resource);
        var azurePostgres = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azurePostgres.WithParameter("administratorLogin", administratorLogin)
                     .WithParameter("administratorLoginPassword", administratorLoginPassword)
                     .WithParameter("databases", () => builder.Resource.Databases);

        if (callback != null)
        {
            callback(azurePostgres);
        }

        return builder;
    }

    /// <summary>
    /// TODO: doc comments
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="administratorLogin"></param>
    /// <param name="administratorLoginPassword"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(this IResourceBuilder<PostgresServerResource> builder, IResourceBuilder<ParameterResource> administratorLogin, IResourceBuilder<ParameterResource> administratorLoginPassword, Action<IResourceBuilder<AzurePostgresResource>>? callback = null)
    {
        var resource = new AzurePostgresResource(builder.Resource);
        var azurePostgres = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azurePostgres.WithParameter("administratorLogin", administratorLogin)
                     .WithParameter("administratorLoginPassword", administratorLoginPassword)
                     .WithParameter("databases", () => builder.Resource.Databases);

        // Used to hold a reference to the azure surrogate for use with the provisioner.
        builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));
        builder.WithConnectionStringRedirection(resource);

        // Remove the container annotation so that DCP doesn't do anything with it.
        if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
        {
            builder.Resource.Annotations.Remove(containerAnnotation);
        }

        if (callback != null)
        {
            callback(azurePostgres);
        }

        return builder;
    }

    private static IResourceBuilder<AzurePostgresResource> ConfigureDefaults(this IResourceBuilder<AzurePostgresResource> builder)
    {
        var resource = builder.Resource;
        return builder.WithManifestPublishingCallback(resource.WriteToManifest)
                      .WithParameter("serverName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName);
    }
}
