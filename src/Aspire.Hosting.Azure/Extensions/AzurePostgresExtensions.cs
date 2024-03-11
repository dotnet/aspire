// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.KeyVaults;
using Azure.Provisioning.PostgreSql;

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

    private static IResourceBuilder<T> WithLoginAndPassword<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<ParameterResource>? administratorLogin,
        IResourceBuilder<ParameterResource>? administratorLoginPassword) where T: AzureBicepResource
    {
        if (administratorLogin is null)
        {
            const string usernameInput = "username";
            // generate a username since a parameter was not provided
            builder.WithAnnotation(new InputAnnotation(usernameInput)
            {
                Default = new GenerateInputDefault
                {
                    MinLength = 10,
                    // just use letters for the username since it can't start with a number
                    Numeric = false,
                    Special = false
                }
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

    internal static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServerConstruct(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin = null,
        IResourceBuilder<ParameterResource>? administratorLoginPassword = null,
        Action<IResourceBuilder<AzurePostgresConstructResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource = null,
        bool useProvisioner = false)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var administratorLogin = new Parameter("administratorLogin");
            var administratorLoginPassword = new Parameter("administratorLoginPassword", isSecure: true);

            var postgres = new PostgreSqlFlexibleServer(construct, administratorLogin, administratorLoginPassword, name: construct.Resource.Name);
            postgres.AssignProperty(x => x.Sku.Name, "'Standard_B1ms'");
            postgres.AssignProperty(x => x.Sku.Tier, "'Burstable'");
            postgres.AssignProperty(x => x.Version, "'16'");
            postgres.AssignProperty(x => x.HighAvailability.Mode, "'Disabled'");
            postgres.AssignProperty(x => x.Storage.StorageSizeInGB, "32");
            postgres.AssignProperty(x => x.Backup.BackupRetentionDays, "7");
            postgres.AssignProperty(x => x.Backup.GeoRedundantBackup, "'Disabled'");
            postgres.AssignProperty(x => x.AvailabilityZone, "'1'");

            postgres.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            // Opens access to all Azure services.
            var azureServicesFirewallRule = new PostgreSqlFirewallRule(construct, "0.0.0.0", "0.0.0.0", postgres, "AllowAllAzureIps");

            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                // Opens access to the Internet.
                var openFirewallRule = new PostgreSqlFirewallRule(construct, "0.0.0.0", "255.255.255.255", postgres, "AllowAllIps");
            }

            List<PostgreSqlFlexibleServerDatabase> sqlDatabases = new List<PostgreSqlFlexibleServerDatabase>();
            foreach (var databaseNames in builder.Resource.Databases)
            {
                var databaseName = databaseNames.Value;
                var pgsqlDatabase = new PostgreSqlFlexibleServerDatabase(construct, postgres, databaseName);
                sqlDatabases.Add(pgsqlDatabase);
            }

            var keyVault = KeyVault.FromExisting(construct, "keyVaultName");
            _ = new KeyVaultSecret(construct, "connectionString", postgres.GetConnectionString(administratorLogin, administratorLoginPassword));

            if (configureResource != null)
            {
                var azureResource = (AzurePostgresConstructResource)construct.Resource;
                var azureResourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(azureResource);
                configureResource(azureResourceBuilder, construct, postgres);
            }
        };

        var resource = new AzurePostgresConstructResource(builder.Resource, configureConstruct);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource)
                                                        .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                                                        .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                                                        .WithManifestPublishingCallback(resource.WriteToManifest)
                                                        .WithLoginAndPassword(administratorLogin, administratorLoginPassword);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            resourceBuilder.WithParameter(AzureBicepResource.KnownParameters.PrincipalType);
        }

        if (useProvisioner)
        {
            // Used to hold a reference to the azure surrogate for use with the provisioner.
            builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));
            builder.WithConnectionStringRedirection(resource);

            // Remove the container annotation so that DCP doesn't do anything with it.
            if (builder.Resource.Annotations.OfType<ContainerImageAnnotation>().SingleOrDefault() is { } containerAnnotation)
            {
                builder.Resource.Annotations.Remove(containerAnnotation);
            }
        }

        return builder;
    }

    /// <summary>
    /// Configures Postgres Server resource to be deployed as Azure Postgres Flexible Server.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</param>
    /// <param name="administratorLogin"></param>
    /// <param name="administratorLoginPassword"></param>
    /// <param name="configureResource">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServerConstruct(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin = null,
        IResourceBuilder<ParameterResource>? administratorLoginPassword = null,
        Action<IResourceBuilder<AzurePostgresConstructResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource = null)
    {
        return builder.PublishAsAzurePostgresFlexibleServerConstruct(
            administratorLogin,
            administratorLoginPassword,
            configureResource,
            useProvisioner: false);
    }

    /// <summary>
    /// Configures resource to use Azure for local development and when doing a deployment via the Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</param>
    /// <param name="administratorLogin"></param>
    /// <param name="administratorLoginPassword"></param>
    /// <param name="configureResource">Callback to configure Azure resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServerConstruct(
        this IResourceBuilder<PostgresServerResource> builder,
        IResourceBuilder<ParameterResource>? administratorLogin = null,
        IResourceBuilder<ParameterResource>? administratorLoginPassword = null,
        Action<IResourceBuilder<AzurePostgresConstructResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource = null)
    {
        return builder.PublishAsAzurePostgresFlexibleServerConstruct(
            administratorLogin,
            administratorLoginPassword,
            configureResource,
            useProvisioner: true);
    }
}
