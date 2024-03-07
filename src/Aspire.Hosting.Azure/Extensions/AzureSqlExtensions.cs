// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Sql;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SQL resources to the application model.
/// </summary>
public static class AzureSqlExtensions
{
    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <param name="callback">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>>? callback = null)
    {
        var resource = new AzureSqlServerResource(builder.Resource);
        var azureSqlDatabase = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azureSqlDatabase.WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                        .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
                        .WithParameter("databases", () => builder.Resource.Databases.Select(x => x.Value));

        if (callback != null)
        {
            callback(azureSqlDatabase);
        }

        return builder;
    }

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <param name="callback">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>>? callback = null)
    {
        var resource = new AzureSqlServerResource(builder.Resource);
        var azureSqlDatabase = builder.ApplicationBuilder.CreateResourceBuilder(resource).ConfigureDefaults();
        azureSqlDatabase.WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                        .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
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
            callback(azureSqlDatabase);
        }

        return builder;
    }

    private static IResourceBuilder<AzureSqlServerResource> ConfigureDefaults(this IResourceBuilder<AzureSqlServerResource> builder)
    {
        var resource = builder.Resource;
        return builder.WithManifestPublishingCallback(resource.WriteToManifest)
                      .WithParameter("serverName", resource.CreateBicepResourceName());
    }

    internal static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabaseConstruct(this IResourceBuilder<SqlServerServerResource> builder, Action<ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource = null, bool useProvisioner = false)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var sqlServer = new SqlServer(construct, builder.Resource.Name);

            sqlServer.AssignProperty(x => x.Administrators.AdministratorType, "'ActiveDirectory'");
            sqlServer.AssignProperty(x => x.Administrators.IsAzureADOnlyAuthenticationEnabled, "true");
            sqlServer.AssignProperty(x => x.Administrators.Sid, construct.PrincipalIdParameter);
            sqlServer.AssignProperty(x => x.Administrators.Login, construct.PrincipalNameParameter);
            sqlServer.AssignProperty(x => x.Administrators.TenantId, "subscription().tenantId");

            var azureServicesFirewallRule = new SqlFirewallRule(construct, sqlServer, "AllowAllAzureIps");
            azureServicesFirewallRule.AssignProperty(x => x.StartIPAddress, "'0.0.0.0'");
            azureServicesFirewallRule.AssignProperty(x => x.EndIPAddress, "'0.0.0.0'");

            if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
            {
                // When in run mode we inject the users identity and we need to specify
                // the principalType.
                sqlServer.AssignProperty(x => x.Administrators.PrincipalType, construct.PrincipalTypeParameter);

                var sqlFirewall = new SqlFirewallRule(construct);
                sqlFirewall.AssignProperty(x => x.StartIPAddress, "'0.0.0.0'");
                sqlFirewall.AssignProperty(x => x.EndIPAddress, "'255.255.255.255'");
            }

            List<SqlDatabase> sqlDatabases = new List<SqlDatabase>();
            foreach (var databaseNames in builder.Resource.Databases)
            {
                var databaseName = databaseNames.Value;
                var sqlDatabase = new SqlDatabase(construct, sqlServer, databaseName);
                sqlDatabases.Add(sqlDatabase);
            }

            sqlServer.AddOutput(x => x.FullyQualifiedDomainName, "sqlServerFqdn");

            if (configureResource != null)
            {
                configureResource(construct, sqlServer, sqlDatabases);
            }
        };

        var resource = new AzureSqlServerConstructResource(builder.Resource, configureConstruct);
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
    /// <param name="configureResource">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabaseConstruct(this IResourceBuilder<SqlServerServerResource> builder, Action<ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource = null)
    {
        return builder.PublishAsAzureSqlDatabaseConstruct(configureResource, useProvisioner: false);
    }

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <param name="configureResource">Callback to customize the Azure resources that will be provisioned in Azure.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabaseConstruct(this IResourceBuilder<SqlServerServerResource> builder, Action<ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource = null)
    {
        return builder.PublishAsAzureSqlDatabaseConstruct(configureResource, useProvisioner: true);
    }
}
