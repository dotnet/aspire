// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an resource for Azure Postgres Flexible Server.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to configure construct.</param>
public class AzurePostgresFlexibleServerResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithConnectionString
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
    internal BicepSecretOutputReference? ConnectionStringSecretOutput { get; set; }

    /// <summary>
    /// Gets the inner PostgresServerResource resource.
    /// 
    /// This is set when RunAsContainer is called on the AzurePostgresFlexibleServerResource resource to create a local PostgreSQL container.
    /// </summary>
    internal PostgresServerResource? InnerResource { get; set; }

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
            (ConnectionStringSecretOutput is not null ? ReferenceExpression.Create($"{ConnectionStringSecretOutput}") :
            ReferenceExpression.Create($"{ConnectionStringOutput}"));

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }

    internal ReferenceExpression GetDatabaseConnectionString(string databaseResourceName, string databaseName)
    {
        // If the server resource is using a secret output, then the database should also use a secret output as well.
        // Note that the bicep template puts each database's connection string in a KeyVault secret.
        if (InnerResource is null && ConnectionStringSecretOutput is not null)
        {
            return ReferenceExpression.Create($"{new BicepSecretOutputReference(GetDatabaseKeyVaultSecretName(databaseResourceName), this)}");
        }

        return ReferenceExpression.Create($"{this};Database={databaseName}");
    }

    internal static string GetDatabaseKeyVaultSecretName(string databaseResourceName) => $"{databaseResourceName}-connectionString";
}
