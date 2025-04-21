// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Sql;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SQL resources to the application model.
/// </summary>
public static class AzureSqlExtensions
{    
    [Obsolete]
    private static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, bool useProvisioner)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ApplicationBuilder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            CreateSqlServer(infrastructure, builder.ApplicationBuilder, builder.Resource.Databases);
        };

        var resource = new AzureSqlServerResource(builder.Resource, configureInfrastructure);
        var azureSqlDatabase = builder.ApplicationBuilder.CreateResourceBuilder(resource);
        azureSqlDatabase.WithManifestPublishingCallback(resource.WriteToManifest);

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
        => PublishAsAzureSqlDatabase(builder, useProvisioner: false);

    /// <summary>
    /// Configures SQL Server resource to be deployed as Azure SQL Database (server).
    /// </summary>
    /// <param name="builder">The builder for the SQL Server resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
    public static IResourceBuilder<SqlServerServerResource> AsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder)
        => PublishAsAzureSqlDatabase(builder, useProvisioner: true);

    /// <summary>
    /// Adds an Azure SQL Database (server) resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlServerResource}"/> builder.</returns>
    public static IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var azureResource = (AzureSqlServerResource)infrastructure.AspireResource;
            CreateSqlServer(infrastructure, builder, azureResource.AzureSqlDatabases);
        };

        var resource = new AzureSqlServerResource(name, configureInfrastructure);
        var azureSqlServer = builder.AddResource(resource)
            .WithAnnotation(new DefaultRoleAssignmentsAnnotation(new HashSet<RoleDefinition>()));

        return azureSqlServer;
    }

    /// <summary>
    /// Adds an Azure SQL Database to the application model.
    /// </summary>
    /// <param name="builder">The builder for the Azure SQL resource.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlDatabaseResource> AddDatabase(this IResourceBuilder<AzureSqlServerResource> builder, [ResourceName] string name, string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        var azureResource = builder.Resource;
        var azureSqlDatabase = new AzureSqlDatabaseResource(name, databaseName, azureResource);
        azureSqlDatabase.SkuName = AzureSqlDatabaseResource.FREE_SKU_NAME;

        builder.Resource.AddDatabase(azureSqlDatabase);

        if (azureResource.InnerResource is null)
        {
            return builder.ApplicationBuilder.AddResource(azureSqlDatabase);
        }
        else
        {
            // need to add the database to the InnerResource
            var innerBuilder = builder.ApplicationBuilder.CreateResourceBuilder(azureResource.InnerResource);
            var innerDb = innerBuilder.AddDatabase(name, databaseName);
            azureSqlDatabase.SetInnerResource(innerDb.Resource);

            // create a builder, but don't add the Azure database to the model because the InnerResource already has it
            return builder.ApplicationBuilder.CreateResourceBuilder(azureSqlDatabase);
        }
    }

    /// <summary>
    /// Configures the Azure SQL Database to be deployed with the specified SKU
    /// </summary>
    /// <param name="builder">The builder for the Azure SQL resource.</param>
    /// <param name="skuName">SKU of the database. If not provided, this defaults to the free database tier.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlDatabaseResource> WithSku(this IResourceBuilder<AzureSqlDatabaseResource> builder, string skuName)
    {
        builder.Resource.SkuName = skuName;
        return builder;
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
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        var azureResource = builder.Resource;
        var azureDatabases = builder.ApplicationBuilder.Resources
            .OfType<AzureSqlDatabaseResource>()
            .Where(db => db.Parent == azureResource)
            .ToDictionary(db => db.Name);

        RemoveAzureResources(builder.ApplicationBuilder, azureResource, azureDatabases);

        var sqlContainer = builder.ApplicationBuilder.AddSqlServer(azureResource.Name);

        azureResource.SetInnerResource(sqlContainer.Resource);

        foreach (var database in azureResource.AzureSqlDatabases)
        {
            if (!azureDatabases.TryGetValue(database.Key, out var existingDb))
            {
                throw new InvalidOperationException($"Could not find a {nameof(AzureSqlDatabaseResource)} with name {database.Key}.");
            }

            var innerDb = sqlContainer.AddDatabase(database.Key, database.Value.DatabaseName);
            existingDb.SetInnerResource(innerDb.Resource);
        }

        configureContainer?.Invoke(sqlContainer);

        return builder;
    }

    private static void RemoveAzureResources(IDistributedApplicationBuilder appBuilder, AzureSqlServerResource azureResource, Dictionary<string, AzureSqlDatabaseResource> azureDatabases)
    {
        appBuilder.Resources.Remove(azureResource);
        foreach (var database in azureDatabases)
        {
            appBuilder.Resources.Remove(database.Value);
        }
    }

    private static void CreateSqlServer(
           AzureResourceInfrastructure infrastructure,
        IDistributedApplicationBuilder distributedApplicationBuilder,
        IReadOnlyDictionary<string, string> databases)
    {
        var sqlServer = CreateSqlServerResourceOnly(infrastructure, distributedApplicationBuilder);

        foreach (var database in databases)
        {
            var bicepIdentifier = Infrastructure.NormalizeBicepIdentifier(database.Key);
            var databaseName = database.Value;
            var sqlDatabase = new SqlDatabase(bicepIdentifier)
            {
                Parent = sqlServer,
                Name = databaseName,
            };
            
            sqlDatabase.Sku = new SqlSku() { Name = AzureSqlDatabaseResource.FREE_DB_SKU };
            sqlDatabase.UseFreeLimit = true;
            sqlDatabase.FreeLimitExhaustionBehavior = FreeLimitExhaustionBehavior.AutoPause;

            infrastructure.Add(sqlDatabase);
        }
    }

    private static void CreateSqlServer(
        AzureResourceInfrastructure infrastructure,
        IDistributedApplicationBuilder distributedApplicationBuilder,
        IReadOnlyDictionary<string, AzureSqlDatabaseResource> databases)
    {
        var sqlServer = CreateSqlServerResourceOnly(infrastructure, distributedApplicationBuilder);

        foreach (var database in databases)
        {
            var bicepIdentifier = Infrastructure.NormalizeBicepIdentifier(database.Key);
            var databaseName = database.Value.DatabaseName;
            var sqlDatabase = new SqlDatabase(bicepIdentifier)
            {
                Parent = sqlServer,
                Name = databaseName,
            };

            if (string.Equals(database.Value.SkuName, AzureSqlDatabaseResource.FREE_SKU_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                sqlDatabase.Sku = new SqlSku() { Name = AzureSqlDatabaseResource.FREE_DB_SKU };
                sqlDatabase.UseFreeLimit = true;
                sqlDatabase.FreeLimitExhaustionBehavior = FreeLimitExhaustionBehavior.AutoPause;
            } else
            {
                sqlDatabase.Sku = new SqlSku() { Name = database.Value.SkuName };
            }

            infrastructure.Add(sqlDatabase);
        }
    }

    private static SqlServer CreateSqlServerResourceOnly(AzureResourceInfrastructure infrastructure,
        IDistributedApplicationBuilder distributedApplicationBuilder)
    {
        var azureResource = (AzureSqlServerResource)infrastructure.AspireResource;

        var sqlServer = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
        (identifier, name) =>
        {
            var resource = SqlServer.FromExisting(identifier);
            resource.Name = name;
            return resource;
        },
        (infrastructure) =>
        {
            // Creating a new SqlServer instance requires an administrator,
            // so we need to create one here using the empty PrincipalId/PrincipalName
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);
            var principalNameParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalName, typeof(string));
            infrastructure.Add(principalNameParameter);

            return new SqlServer(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Administrators = new ServerExternalAdministrator()
                {
                    AdministratorType = SqlAdministratorType.ActiveDirectory,
                    IsAzureADOnlyAuthenticationEnabled = true,
                    Sid = principalIdParameter,
                    Login = principalNameParameter,
                    TenantId = BicepFunction.GetSubscription().TenantId
                },
                Version = "12.0",
                PublicNetworkAccess = ServerNetworkAccessFlag.Enabled,
                MinTlsVersion = SqlMinimalTlsVersion.Tls1_2,
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
        });

        infrastructure.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllAzureIps")
        {
            Parent = sqlServer,
            Name = "AllowAllAzureIps",
            StartIPAddress = "0.0.0.0",
            EndIPAddress = "0.0.0.0"
        });

        if (distributedApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Avoid mutating properties on existing resources.
            if (!sqlServer.IsExistingResource)
            {
                // When in run mode we inject the users identity and we need to specify
                // the principalType.
                var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
                infrastructure.Add(principalTypeParameter);
                sqlServer.Administrators.PrincipalType = principalTypeParameter;
            }

            infrastructure.Add(new SqlFirewallRule("sqlFirewallRule_AllowAllIps")
            {
                Parent = sqlServer,
                Name = "AllowAllIps",
                StartIPAddress = "0.0.0.0",
                EndIPAddress = "255.255.255.255"
            });
        }

        infrastructure.Add(new ProvisioningOutput("sqlServerFqdn", typeof(string)) { Value = sqlServer.FullyQualifiedDomainName });

        // We need to output name to externalize role assignments.
        infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = sqlServer.Name });

        return sqlServer;
    }

    internal static SqlServerAzureADAdministrator AddActiveDirectoryAdministrator(AzureResourceInfrastructure infra, SqlServer sqlServer, BicepValue<Guid> principalId, BicepValue<string> principalName)
    {
        var admin = new SqlServerAzureADAdministratorWorkaround($"{sqlServer.BicepIdentifier}_admin")
        {
            ParentOverride = sqlServer,
            LoginOverride = principalName,
            SidOverride = principalId
        };
        infra.Add(admin);
        return admin;
    }

    /// <remarks>
    /// Workaround for issue using SqlServerAzureADAdministrator.
    /// See https://github.com/Azure/azure-sdk-for-net/issues/48364 for more information.
    /// </remarks>
    private sealed class SqlServerAzureADAdministratorWorkaround(string bicepIdentifier) : SqlServerAzureADAdministrator(bicepIdentifier)
    {
        private BicepValue<string>? _name;
        private BicepValue<string>? _login;
        private BicepValue<Guid>? _sid;
        private ResourceReference<SqlServer>? _parent;

        /// <summary>
        /// Login name of the server administrator.
        /// </summary>
        public BicepValue<string> LoginOverride
        {
            get
            {
                Initialize();
                return _login!;
            }
            set
            {
                Initialize();
                _login!.Assign(value);
            }
        }

        /// <summary>
        /// SID (object ID) of the server administrator.
        /// </summary>
        public BicepValue<Guid> SidOverride
        {
            get
            {
                Initialize();
                return _sid!;
            }
            set
            {
                Initialize();
                _sid!.Assign(value);
            }
        }

        /// <summary>
        /// Parent resource of the server administrator.
        /// </summary>
        public SqlServer? ParentOverride
        {
            get
            {
                Initialize();
                return _parent!.Value;
            }
            set
            {
                Initialize();
                _parent!.Value = value;
            }
        }

        private static BicepValue<string> GetNameDefaultValue()
        {
            return new StringLiteralExpression("ActiveDirectory");
        }

        protected override void DefineProvisionableProperties()
        {
            _name = DefineProperty("Name", ["name"], defaultValue: GetNameDefaultValue());
            _login = DefineProperty<string>("Login", ["properties", "login"]);
            _sid = DefineProperty<Guid>("Sid", ["properties", "sid"]);
            _parent = DefineResource<SqlServer>("Parent", ["parent"], isOutput: false, isRequired: true);
        }
    }
}
