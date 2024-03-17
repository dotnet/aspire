// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics.CodeAnalysis;
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
    private static IResourceBuilder<T> WithLoginAndPassword<T>(this IResourceBuilder<T> builder, PostgresServerResource postgresResource) where T : AzureBicepResource
    {
        if (postgresResource.UserNameParameter is null)
        {
            var generatedUserName = new GenerateParameterDefault
            {
                MinLength = 10,
                // just use letters for the username since it can't start with a number
                Numeric = false,
                Special = false
            };

            var userParam = ParameterResourceBuilderExtensions.CreateGeneratedParameter(
                builder.ApplicationBuilder, $"{builder.Resource.Name}-username", secret: false, generatedUserName);

            builder.WithParameter("administratorLogin", userParam);
        }
        else
        {
            builder.WithParameter("administratorLogin", postgresResource.UserNameParameter);
        }

        builder.WithParameter("administratorLoginPassword", postgresResource.PasswordParameter);

        return builder;
    }

    internal static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServerInternal(
        this IResourceBuilder<PostgresServerResource> builder,
        Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource,
        bool useProvisioner = false)
    {
        builder.ApplicationBuilder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var administratorLogin = new Parameter("administratorLogin");
            var administratorLoginPassword = new Parameter("administratorLoginPassword", isSecure: true);

            var postgres = new PostgreSqlFlexibleServer(construct, administratorLogin, administratorLoginPassword, name: construct.Resource.Name, storageSizeInGB: 32);
            postgres.AssignProperty(x => x.Sku.Name, "'Standard_B1ms'");
            postgres.AssignProperty(x => x.Sku.Tier, "'Burstable'");
            postgres.AssignProperty(x => x.Version, "'16'");
            postgres.AssignProperty(x => x.HighAvailability.Mode, "'Disabled'");
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
            _ = new KeyVaultSecret(construct, "connectionString", postgres.GetConnectionString(administratorLogin, administratorLoginPassword), keyVault);

            var azureResource = (AzurePostgresResource)construct.Resource;
            var azureResourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, postgres);
        };

        var resource = new AzurePostgresResource(builder.Resource, configureConstruct);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource)
                                                        .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                                                        .WithManifestPublishingCallback(resource.WriteToManifest)
                                                        .WithLoginAndPassword(builder.Resource);

        builder.WithAnnotation(new AzureBicepResourceAnnotation(resource));

        if (useProvisioner)
        {
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
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.PostgreSql.PostgreSqlFlexibleServer"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder,
        Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource)
    {
        return builder.PublishAsAzurePostgresFlexibleServerInternal(
            configureResource,
            useProvisioner: false);
    }

    /// <summary>
    /// Configures Postgres Server resource to be deployed as Azure Postgres Flexible Server.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    public static IResourceBuilder<PostgresServerResource> PublishAsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.PublishAsAzurePostgresFlexibleServer(null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Configures resource to use Azure for local development and when doing a deployment via the Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AsAzurePostgresFlexibleServer(null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    /// <summary>
    /// Configures resource to use Azure for local development and when doing a deployment via the Azure Developer CLI.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.PostgreSql.PostgreSqlFlexibleServer"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PostgresServerResource}"/> builder.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<PostgresServerResource> AsAzurePostgresFlexibleServer(
        this IResourceBuilder<PostgresServerResource> builder,
        Action<IResourceBuilder<AzurePostgresResource>, ResourceModuleConstruct, PostgreSqlFlexibleServer>? configureResource)
    {
        return builder.PublishAsAzurePostgresFlexibleServerInternal(
            configureResource,
            useProvisioner: true);
    }
}
