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
    [Obsolete]
    private static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource, bool useProvisioner = false)
    {
        builder.ApplicationBuilder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var (sqlServer, sqlDatabases) = CreateSqlServer(construct, builder.ApplicationBuilder, builder.Resource.Databases);

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
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
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
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
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
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
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
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<IResourceBuilder<AzureSqlServerResource>, ResourceModuleConstruct, SqlServer, IEnumerable<SqlDatabase>>? configureResource)
    {
        return builder.PublishAsAzureSqlDatabase(configureResource, useProvisioner: true);
    }

    /// <summary>
    /// Adds an Azure SQL Database (server) resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlServerResource}"/> builder.</returns>
    public static IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(this IDistributedApplicationBuilder builder, string name)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var azureResource = (AzureSqlServerResource)construct.Resource;
            CreateSqlServer(construct, builder, azureResource.Databases);
        };

        var resource = new AzureSqlServerResource(name, configureConstruct);
        var azureSqlServer = builder.AddResource(resource)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
            .WithManifestPublishingCallback(resource.WriteToManifest);

        if (builder.ExecutionContext.IsRunMode)
        {
            // When in run mode we inject the users identity and we need to specify
            // the principalType.
            azureSqlServer.WithParameter(AzureBicepResource.KnownParameters.PrincipalType);
        }

        return azureSqlServer;
    }

    /// <summary>
    /// Adds an Azure SQL Database to the application model.
    /// </summary>
    /// <param name="builder">The builder for the Azure SQL resource.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlDatabaseResource> AddDatabase(this IResourceBuilder<AzureSqlServerResource> builder, string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        var azureResource = builder.Resource;
        var azureSqlDatabase = new AzureSqlDatabaseResource(name, databaseName, azureResource);

        builder.Resource.AddDatabase(name, databaseName);

        if (azureResource.InnerResource is null)
        {
            return builder.ApplicationBuilder.AddResource(azureSqlDatabase);
        }
        else
        {
            // need to add the database to the InnerResource
            var innerBuilder = builder.ApplicationBuilder.CreateResourceBuilder(azureResource.InnerResource);
            innerBuilder.AddDatabase(name, databaseName);

            // create a builder, but don't add the Azure database to the model because the InnerResource already has it
            return builder.ApplicationBuilder.CreateResourceBuilder(azureSqlDatabase);
        }
    }

    /// <summary>
    /// Configures an Azure SQL Database (server) resource to run locally in a container.
    /// </summary>
    /// <param name="builder">The builder for the Azure SQL resource.</param>
    /// <param name="configureContainer">Callback that exposes underlying container to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlServerResource}"/> builder.</returns>
    /// <example>
    /// The following example creates an Azure SQL Database (server) resource that runs locally in a
    /// SQL Server container and referencing that resource in a .NET project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var data = builder.AddAzureSqlServer("data")
    ///     .RunAsContainer();
    ///
    /// builder.AddProject&lt;Projects.ProductService&gt;()
    ///     .WithReference(data);
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<AzureSqlServerResource> RunAsContainer(this IResourceBuilder<AzureSqlServerResource> builder, Action<IResourceBuilder<SqlServerServerResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        var azureResource = builder.Resource;
        var azureDatabases = builder.ApplicationBuilder.Resources
            .OfType<AzureSqlDatabaseResource>()
            .Where(db => db.Parent == azureResource)
            .ToDictionary(db => db.Name);

        RemoveAzureResources(builder.ApplicationBuilder, azureResource);

        var sqlContainer = builder.ApplicationBuilder.AddSqlServer(azureResource.Name);

        azureResource.SetInnerResource(sqlContainer.Resource);

        foreach (var database in azureResource.Databases)
        {
            if (!azureDatabases.TryGetValue(database.Key, out var existingDb))
            {
                throw new InvalidOperationException($"Could not find a {nameof(AzureSqlDatabaseResource)} with name {database.Key}.");
            }

            var innerDb = sqlContainer.AddDatabase(database.Key, database.Value);
            existingDb.SetInnerResource(innerDb.Resource);
        }

        configureContainer?.Invoke(sqlContainer);

        return builder;
    }

    private static void RemoveAzureResources(IDistributedApplicationBuilder appBuilder, AzureSqlServerResource azureResource)
    {
        List<IResource> resourcesToRemove = [azureResource];
        resourcesToRemove.AddRange(
            appBuilder.Resources.OfType<AzureSqlDatabaseResource>()
                .Where(db => db.Parent == azureResource));

        foreach (var resource in resourcesToRemove)
        {
            appBuilder.Resources.Remove(resource);
        }
    }

    private static (SqlServer Server, List<SqlDatabase> Databases) CreateSqlServer(
        ResourceModuleConstruct construct,
        IDistributedApplicationBuilder distributedApplicationBuilder,
        IReadOnlyDictionary<string, string> databases)
    {
        var sqlServer = new SqlServer(construct.Resource.GetBicepIdentifier())
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

        construct.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllAzureIps")
        {
            Parent = sqlServer,
            Name = "AllowAllAzureIps",
            StartIPAddress = "0.0.0.0",
            EndIPAddress = "0.0.0.0"
        });

        if (distributedApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // When in run mode we inject the users identity and we need to specify
            // the principalType.
            sqlServer.Administrators.Value!.PrincipalType = construct.PrincipalTypeParameter;

            construct.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllIps")
            {
                Parent = sqlServer,
                Name = "AllowAllIps",
                StartIPAddress = "0.0.0.0",
                EndIPAddress = "255.255.255.255"
            });
        }

        var sqlDatabases = new List<SqlDatabase>();
        foreach (var databaseNames in databases)
        {
            var identifierName = Infrastructure.NormalizeIdentifierName(databaseNames.Key);
            var databaseName = databaseNames.Value;
            var sqlDatabase = new SqlDatabase(identifierName)
            {
                Parent = sqlServer,
                Name = databaseName
            };
            construct.Add(sqlDatabase);
            sqlDatabases.Add(sqlDatabase);
        }

        construct.Add(new ProvisioningOutput("sqlServerFqdn", typeof(string)) { Value = sqlServer.FullyQualifiedDomainName });

        return (sqlServer, sqlDatabases);
    }
}
