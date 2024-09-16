// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Sql;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SQL resources to the application model.
/// </summary>
public static class AzureSqlExtensions
{
    internal static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource, bool useProvisioner = false)
    {
        builder.ApplicationBuilder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var sqlServer = new SqlServer(builder.Resource.Name)
            {
                Administrators = new ServerExternalAdministrator()
                {
                    AdministratorType = SqlAdministratorType.ActiveDirectory,
                    IsAzureADOnlyAuthenticationEnabled = true,
                    Sid = construct.PrincipalIdParameter,
                    Login = construct.PrincipalNameParameter,
                    TenantId = BicepFunction.GetSubscription().TenantId
                },
                Version = "12.0",
                PublicNetworkAccess = ServerNetworkAccessFlag.Enabled,
                MinTlsVersion = SqlMinimalTlsVersion.Tls1_2,
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(sqlServer);

            construct.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllAzureIps", sqlServer.ResourceVersion)
            {
                Parent = sqlServer,
                Name = "AllowAllAzureIps",
                StartIPAddress = "0.0.0.0",
                EndIPAddress = "0.0.0.0"
            });

            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                // When in run mode we inject the users identity and we need to specify
                // the principalType.
                sqlServer.Administrators.Value!.PrincipalType = construct.PrincipalTypeParameter;

                construct.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllIps", sqlServer.ResourceVersion)
                {
                    Parent = sqlServer,
                    Name = "AllowAllIps",
                    StartIPAddress = "0.0.0.0",
                    EndIPAddress = "255.255.255.255"
                });
            }

            List<SqlDatabase> sqlDatabases = new List<SqlDatabase>();
            foreach (var databaseNames in builder.Resource.Databases)
            {
                var resourceName = databaseNames.Key;
                var databaseName = databaseNames.Value;
                var sqlDatabase = new SqlDatabase(resourceName, sqlServer.ResourceVersion)
                {
                    Parent = sqlServer,
                    Name = databaseName
                };
                construct.Add(sqlDatabase);
                sqlDatabases.Add(sqlDatabase);
            }

            construct.Add(new BicepOutput("sqlServerFqdn", typeof(string)) { Value = sqlServer.FullyQualifiedDomainName });

            var resource = (AzureSqlServerResource)construct.Resource;
            var resourceBuilder = builder.ApplicationBuilder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, sqlServer, sqlDatabases);
        };

        var resource = new AzureSqlServerResource(builder.Resource, configureConstruct);
        var azureSqlDatabase = builder.ApplicationBuilder.CreateResourceBuilder(resource);
        azureSqlDatabase.WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                        .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
                        .WithManifestPublishingCallback(resource.WriteToManifest);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            azureSqlDatabase.WithParameter(AzureBicepResource.KnownParameters.PrincipalType);
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
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder)
    {
        return builder.PublishAsAzureSqlDatabase(null);
    }

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.Sql.SqlServer"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource)
    {
        return builder.PublishAsAzureSqlDatabase(configureResource, useProvisioner: false);
    }

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder)
    {
        return builder.AsAzureSqlDatabase(null);
    }

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.Sql.SqlServer"/> resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource)
    {
        return builder.PublishAsAzureSqlDatabase(configureResource, useProvisioner: true);
    }
}
