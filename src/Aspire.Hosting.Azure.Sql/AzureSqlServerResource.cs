// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Hashing;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Resources;
using Azure.Provisioning.Roles;
using Azure.Provisioning.Sql;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Sql Server resource.
/// </summary>
public class AzureSqlServerResource : AzureProvisioningResource, IResourceWithConnectionString
{
    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);
    private readonly bool _createdWithInnerResource;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    public AzureSqlServerResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlServerResource"/> class.
    /// </summary>
    /// <param name="innerResource">The <see cref="SqlServerServerResource"/> that this resource wraps.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AzureSqlExtensions.AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
    public AzureSqlServerResource(SqlServerServerResource innerResource, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(innerResource.Name, configureInfrastructure)
    {
        InnerResource = innerResource;
        _createdWithInnerResource = true;
    }

    /// <summary>
    /// Gets the fully qualified domain name (FQDN) output reference from the bicep template for the Azure SQL Server resource.
    /// </summary>
    public BicepOutputReference FullyQualifiedDomainName => new("sqlServerFqdn", this);

    private BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the connection template for the manifest for the Azure SQL Server resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            // When the resource was created with an InnerResource (using AsAzure or PublishAsAzure extension methods)
            // the InnerResource will have a ConnectionStringRedirectAnnotation back to this resource. In that case, don't
            // use the InnerResource's ConnectionString, or else it will infinite loop and stack overflow.
            ReferenceExpression? result = null;
            if (!_createdWithInnerResource)
            {
                result = InnerResource?.ConnectionStringExpression;
            }

            return result ??
                ReferenceExpression.Create($"Server=tcp:{FullyQualifiedDomainName},1433;Encrypt=True;Authentication=\"Active Directory Default\"");
        }
    }

    /// <summary>
    /// Gets the inner SqlServerServerResource resource.
    /// 
    /// This is set when RunAsContainer is called on the AzureSqlServerResource resource to create a local SQL Server container.
    /// </summary>
    internal SqlServerServerResource? InnerResource { get; private set; }

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => InnerResource?.Annotations ?? base.Annotations;

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }

    internal void SetInnerResource(SqlServerServerResource innerResource)
    {
        // Copy the annotations to the inner resource before making it the inner resource
        foreach (var annotation in Annotations)
        {
            innerResource.Annotations.Add(annotation);
        }

        InnerResource = innerResource;
    }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = SqlServer.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }

    /// <inheritdoc/>
    public override void AddRoleAssignments(IAddRoleAssignmentsContext roleAssignmentContext)
    {
        var infra = roleAssignmentContext.Infrastructure;
        var sqlserver = (SqlServer)AddAsExistingResource(infra);

        // The name of the user assigned identity for the container app
        var principalName = roleAssignmentContext.PrincipalName;

        // Get a reference to the user assigned identity that is used for the deployment
        // c.f. AzureContainerAppExtensions.AddAzureContainerAppEnvironment()
        var userManagedIdentity = UserAssignedIdentity.FromExisting("mi");
        infra.Add(userManagedIdentity);

        // Identity requires Directory Reader role to be added as a user
        // https://learn.microsoft.com/azure/azure-sql/database/authentication-aad-directory-readers-role

        var ra = new RoleAssignment(Infrastructure.NormalizeBicepIdentifier($"roleAssignment"))
        {
            Scope = new IdentifierExpression(sqlserver.BicepIdentifier),
            RoleDefinitionId = BicepFunction.GetSubscriptionResourceId("Microsoft.Authorization/roleDefinitions", "88d8e3e3-8f55-4a1e-953a-9b9898b8876b"),
            PrincipalId = roleAssignmentContext.PrincipalId,
            PrincipalType = roleAssignmentContext.PrincipalType,
        };
        infra.Add(ra);

        foreach (var database in Databases.Keys)
        {
            var hash = new XxHash3();
            hash.Append(Encoding.UTF8.GetBytes(database));
            hash.Append(Encoding.UTF8.GetBytes(infra.AspireResource.GetBicepIdentifier()));

            var scriptResource = new SqlServerScriptProvisioningResource($"dbroles_{Convert.ToHexString(hash.GetCurrentHash()).ToLowerInvariant()}");

            scriptResource.Identity.IdentityType = ArmDeploymentScriptManagedIdentityType.UserAssigned;
            scriptResource.Identity.UserAssignedIdentities["${mi.id}"] = new UserAssignedIdentityDetails();

            scriptResource.EnvironmentVariables.Add(new ContainerAppEnvironmentVariable() { Name = "DBNAME", Value = database });
            scriptResource.EnvironmentVariables.Add(new ContainerAppEnvironmentVariable() { Name = "DBSERVER", Value = sqlserver.FullyQualifiedDomainName });
            scriptResource.EnvironmentVariables.Add(new ContainerAppEnvironmentVariable() { Name = "IDENTITY", Value = principalName });

            scriptResource.ScriptContent = $$"""
                    echo "Downloading go-sqlcmd"
                    wget https://github.com/microsoft/go-sqlcmd/releases/download/v1.8.2/sqlcmd-linux-amd64.tar.bz2
                    tar x -f sqlcmd-linux-amd64.tar.bz2 -C .
                    echo "Creating database roles for '${DBNAME}' on '${DBSERVER}' on user assigned identity '${IDENTITY}'"
                    ./sqlcmd -S ${DBSERVER} -d ${DBNAME} -G -Q "CREATE USER [${IDENTITY}] FROM EXTERNAL PROVIDER;"
                    echo "Assign db_datareader"
                    ./sqlcmd -S ${DBSERVER} -d ${DBNAME} -G -Q "ALTER ROLE db_datareader ADD MEMBER [${IDENTITY}];"
                    echo "Assign db_datawriter"
                    ./sqlcmd -S ${DBSERVER} -d ${DBNAME} -G -Q "ALTER ROLE db_datawriter ADD MEMBER [${IDENTITY}];"
                    echo "Done"
                    """;

            infra.Add(scriptResource);
        }
    }
}
