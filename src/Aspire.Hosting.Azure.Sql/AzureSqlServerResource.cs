// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
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

    private BicepOutputReference AdminName => new("sqlServerAdminName", this);

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

        var sqlServerAdmin = UserAssignedIdentity.FromExisting("sqlServerAdmin");
        sqlServerAdmin.Name = AdminName.AsProvisioningParameter(infra);
        infra.Add(sqlServerAdmin);

        foreach (var database in Databases.Keys)
        {
            var uniqueScriptIdentifier = Infrastructure.NormalizeBicepIdentifier($"{this.GetBicepIdentifier()}_{database}");
            var scriptResource = new SqlServerScriptProvisioningResource($"script_{uniqueScriptIdentifier}")
            {
                Name = BicepFunction.Take(BicepFunction.Interpolate($"script-{BicepFunction.GetUniqueString(this.GetBicepIdentifier(), roleAssignmentContext.PrincipalName, new StringLiteralExpression(database), BicepFunction.GetResourceGroup().Id)}"), 24),
                Kind = "AzurePowerShell",
                AZPowerShellVersion = "7.4"
            };

            // Run the script as the administrator

            var id = BicepFunction.Interpolate($"{sqlServerAdmin.Id}").Compile().ToString();
            scriptResource.Identity.IdentityType = ArmDeploymentScriptManagedIdentityType.UserAssigned;
            scriptResource.Identity.UserAssignedIdentities[id] = new UserAssignedIdentityDetails();

            // Script don't support Bicep expression, they need to be passed as ENVs
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "DBNAME", Value = database });
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "DBSERVER", Value = sqlserver.FullyQualifiedDomainName });
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "PRINCIPALNAME", Value = roleAssignmentContext.PrincipalName });
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "PRINCIPALTYPE", Value = roleAssignmentContext.PrincipalType });
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "PRINCIPALID", Value = roleAssignmentContext.PrincipalId });
            scriptResource.EnvironmentVariables.Add(new EnvironmentVariable() { Name = "CLIENTID", Value = roleAssignmentContext.ClientId });

            scriptResource.ScriptContent = $$"""
                $sqlServerFqdn = "$env:DBSERVER"
                $sqlDatabaseName = "$env:DBNAME"
                $principalName = "$env:PRINCIPALNAME"
                $id = If ("$env:PRINCIPALTYPE" -eq "User") {"$env:PRINCIPALID"} Else {"$env:CLIENTID"}

                # Install SqlServer module
                Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
                Import-Module SqlServer

                $sqlCmd = @"
                DECLARE @name SYSNAME = '$principalName';
                DECLARE @id UNIQUEIDENTIFIER = '$id';
                
                -- Convert the guid to the right type
                DECLARE @castId NVARCHAR(MAX) = CONVERT(VARCHAR(MAX), CONVERT (VARBINARY(16), @id), 1);
                
                -- Construct command: CREATE USER [@name] WITH SID = @castId, TYPE = E;
                DECLARE @cmd NVARCHAR(MAX) = N'CREATE USER [' + @name + '] WITH SID = ' + @castId + ', TYPE = E;'
                EXEC (@cmd);
                
                -- Assign roles to the new user
                DECLARE @role1 NVARCHAR(MAX) = N'ALTER ROLE db_owner ADD MEMBER [' + @name + ']';
                EXEC (@role1);
                
                "@
                # Note: the string terminator must not have whitespace before it, therefore it is not indented.

                Write-Host $sqlCmd

                $connectionString = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;"

                Invoke-Sqlcmd -ConnectionString $connectionString -Query $sqlCmd
                """;

            infra.Add(scriptResource);
        }
    }
}
