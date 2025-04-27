// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.PostgreSql;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an resource for Azure Postgres Flexible Server.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure infrastructure.</param>
public class AzurePostgresFlexibleServerResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString
{
    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);

    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for the Azure Postgres Flexible Server.
    /// 
    /// This is used when Entra ID authentication is used. The connection string is an output of the bicep template.
    /// </summary>
    private BicepOutputReference ConnectionStringOutput => new("connectionString", this);

    /// <summary>
    /// Gets the "connectionString" secret output reference from the bicep template for the Azure Postgres Flexible Server.
    ///
    /// This is set when password authentication is used. The connection string is stored in a secret in the Azure Key Vault.
    /// </summary>
    internal IAzureKeyVaultSecretReference? ConnectionStringSecretOutput { get; set; }

    private BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets a value indicating whether the resource uses password authentication.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ConnectionStringSecretOutput))]
    public bool UsePasswordAuthentication => ConnectionStringSecretOutput is not null;

    /// <summary>
    /// Gets the inner PostgresServerResource resource.
    /// 
    /// This is set when RunAsContainer is called on the AzurePostgresFlexibleServerResource resource to create a local PostgreSQL container.
    /// </summary>
    internal PostgresServerResource? InnerResource { get; private set; }

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => InnerResource?.Annotations ?? base.Annotations;

    /// <summary>
    /// Gets or sets the parameter that contains the PostgreSQL server user name.
    /// </summary>
    internal ParameterResource? UserNameParameter { get; set; }

    /// <summary>
    /// Gets or sets the parameter that contains the PostgreSQL server password.
    /// </summary>
    internal ParameterResource? PasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection template for the manifest for the Azure Postgres Flexible Server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        InnerResource?.ConnectionStringExpression ??
            (UsePasswordAuthentication ?
                ReferenceExpression.Create(ConnectionStringSecretOutput) :
                ReferenceExpression.Create(ConnectionStringOutput));

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }

    internal void SetInnerResource(PostgresServerResource innerResource)
    {
        // Copy the annotations to the inner resource before making it the inner resource
        foreach (var annotation in Annotations)
        {
            innerResource.Annotations.Add(annotation);
        }

        InnerResource = innerResource;
    }

    internal ReferenceExpression GetDatabaseConnectionString(string databaseResourceName, string databaseName)
    {
        // If the server resource is using a secret output, then the database should also use a secret output as well.
        // Note that the bicep template puts each database's connection string in a KeyVault secret.
        if (InnerResource is null && ConnectionStringSecretOutput is not null)
        {
            return ReferenceExpression.Interpolate($"{ConnectionStringSecretOutput.Resource.GetSecret(GetDatabaseKeyVaultSecretName(databaseResourceName))}");
        }

        return ReferenceExpression.Interpolate($"{this};Database={databaseName}");
    }

    internal static string GetDatabaseKeyVaultSecretName(string databaseResourceName) => $"connectionstrings--{databaseResourceName}";

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = PostgreSqlFlexibleServer.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }

    /// <inheritdoc/>
    public override void AddRoleAssignments(IAddRoleAssignmentsContext roleAssignmentContext)
    {
        Debug.Assert(!UsePasswordAuthentication, "AddRoleAssignments should not be called when using UsePasswordAuthentication");

        var infra = roleAssignmentContext.Infrastructure;
        var postgres = (PostgreSqlFlexibleServer)AddAsExistingResource(infra);

        var principalType = ConvertPrincipalTypeDangerously(roleAssignmentContext.PrincipalType);
        var principalId = roleAssignmentContext.PrincipalId;
        var principalName = roleAssignmentContext.PrincipalName;

        AzurePostgresExtensions.AddActiveDirectoryAdministrator(infra, postgres, principalId, principalType, principalName);
    }

    // Assumes original has a value in PostgreSqlFlexibleServerPrincipalType, will fail at runtime if not
    static BicepValue<PostgreSqlFlexibleServerPrincipalType> ConvertPrincipalTypeDangerously(BicepValue<RoleManagementPrincipalType> original) =>
        original.Compile();
}
