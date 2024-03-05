// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Postgres resources to the application model.
/// </summary>
public static class AzurePostgresExtensions
{
    /// <summary>
    /// Configures Postgres resource to be deployed as Azure Postgres Flexible Server when deployed using Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The builder for the Postgres resource.</param>
    /// <param name="administratorLogin">Parameter containing the administrator username for the server that will be provisioned in Azure.</param>
    /// <param name="administratorLoginPassword">Parameter containing the administrator password for the server that will be provisioned in Azure.</param>
    /// <param name="callback">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns></returns>
    public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin = null,
        IResourceBuilder<ParameterResource>? administratorLoginPassword = null,
        Action<IResourceBuilder<AzurePostgresResource>>? callback = null)
    {
        var resource = new AzurePostgresResource(builder.Resource);
        var azurePostgres = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azurePostgres.WithLoginAndPassword(administratorLogin, administratorLoginPassword)
                     .WithParameter("databases", () => builder.Resource.Databases.Select(x => x.Value));

        if (callback != null)
        {
            callback(azurePostgres);
        }

        return builder;
    }

    /// <summary>
    /// Configures Postgres resource to be deployed as Azure Postgres Flexible Server when deployed using Azure Developer CLI and when the Azure Provisioner is used for local development.
    /// </summary>
    /// <param name="builder">The builder for the Postgres resource.</param>
    /// <param name="administratorLogin">Parameter containing the administrator username for the server that will be provisioned in Azure.</param>
    /// <param name="administratorLoginPassword">Parameter containing the administrator password for the server that will be provisioned in Azure.</param>
    /// <param name="callback">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns></returns>
    public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin = null,
        IResourceBuilder<ParameterResource>? administratorLoginPassword = null,
        Action<IResourceBuilder<AzurePostgresResource>>? callback = null)
    {
        var resource = new AzurePostgresResource(builder.Resource);
        var azurePostgres = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azurePostgres.WithLoginAndPassword(administratorLogin, administratorLoginPassword)
                     .WithParameter("databases", () => builder.Resource.Databases.Select(x => x.Value));

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

    private static IResourceBuilder<AzurePostgresResource> WithLoginAndPassword(
        this IResourceBuilder<AzurePostgresResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin,
        IResourceBuilder<ParameterResource>? administratorLoginPassword)
    {
        if (administratorLogin is null)
        {
            const string usernameInput = "username";
            // generate a username since a parameter was not provided
            builder.WithAnnotation(new InputAnnotation(usernameInput)
            {
                Default = new GenerateInputDefault { MinLength = 10 }
            });

            builder.WithParameter("administratorLogin", new InputReference(builder.Resource, usernameInput));
        }
        else
        {
            builder.WithParameter("administratorLogin", administratorLogin);
        }

        if (administratorLoginPassword is null)
        {
            // generate a password since a parameter was not provided. Use the existing "password" input from the underlying PostgresServerResource
            builder.WithParameter("administratorLoginPassword", new InputReference(builder.Resource, "password"));
        }
        else
        {
            builder.WithParameter("administratorLoginPassword", administratorLoginPassword);
        }

        return builder;
    }
}
