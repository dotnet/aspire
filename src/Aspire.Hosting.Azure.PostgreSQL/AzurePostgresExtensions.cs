// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
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
            var administratorLogin = new BicepParameter("administratorLogin", typeof(string));
            construct.Add(administratorLogin);

            var administratorLoginPassword = new BicepParameter("administratorLoginPassword", typeof(string)) { IsSecure = true };
            construct.Add(administratorLoginPassword);

            var kvNameParam = new BicepParameter("keyVaultName", typeof(string));
            construct.Add(kvNameParam);

            var keyVault = KeyVaultService.FromExisting("keyVault");
            keyVault.Name = kvNameParam;
            construct.Add(keyVault);

            var postgres = new PostgreSqlFlexibleServer(construct.Resource.Name)
            {
                StorageSizeInGB = 32,
                AdministratorLogin = administratorLogin,
                AdministratorLoginPassword = administratorLoginPassword,
                Sku = new PostgreSqlFlexibleServerSku()
                {
                    Name = "Standard_B1ms",
                    Tier = PostgreSqlFlexibleServerSkuTier.Burstable
                },
                Version = new StringLiteral("16"),
                HighAvailability = new PostgreSqlFlexibleServerHighAvailability()
                {
                    Mode = PostgreSqlFlexibleServerHighAvailabilityMode.Disabled
                },
                Backup = new PostgreSqlFlexibleServerBackupProperties()
                {
                    BackupRetentionDays = 7,
                    GeoRedundantBackup = PostgreSqlFlexibleServerGeoRedundantBackupEnum.Disabled
                },
                AvailabilityZone = "1",
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(postgres);

            // Opens access to all Azure services.
            construct.Add(new PostgreSqlFlexibleServerFirewallRule("postgreSqlFirewallRule_AllowAllAzureIps", postgres.ResourceVersion)
            {
                Parent = postgres,
                Name = "AllowAllAzureIps",
                StartIPAddress = new IPAddress([0, 0, 0, 0]),
                EndIPAddress = new IPAddress([0, 0, 0, 0])
            });

            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                // Opens access to the Internet.
                construct.Add(new PostgreSqlFlexibleServerFirewallRule("postgreSqlFirewallRule_AllowAllIps", postgres.ResourceVersion)
                {
                    Parent = postgres,
                    Name = "AllowAllIps",
                    StartIPAddress = new IPAddress([0, 0, 0, 0]),
                    EndIPAddress = new IPAddress([255, 255, 255, 255])
                });
            }

            foreach (var databaseNames in builder.Resource.Databases)
            {
                var resourceName = databaseNames.Key;
                var databaseName = databaseNames.Value;
                var pgsqlDatabase = new PostgreSqlFlexibleServerDatabase(resourceName, postgres.ResourceVersion)
                {
                    Parent = postgres,
                    Name = databaseName
                };
                construct.Add(pgsqlDatabase);
            }

            var secret = new KeyVaultSecret("connectionString")
            {
                Parent = keyVault,
                Name = "connectionString",
                Properties = new SecretProperties
                {
                    Value = BicepFunction.Interpolate($"Host={postgres.FullyQualifiedDomainName};Username={administratorLogin};Password={administratorLoginPassword}")
                }
            };
            construct.Add(secret);

            var azureResource = (AzurePostgresResource)construct.Resource;
            var azureResourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, postgres);
        };

        var resource = new AzurePostgresResource(builder.Resource, configureConstruct);
        var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource)
                                                        .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                                                        .WithManifestPublishingCallback(resource.WriteToManifest)
                                                        .WithLoginAndPassword(builder.Resource);

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
