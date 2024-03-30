// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an resource for Azure Postgres Flexible Server.
/// </summary>
/// <param name="innerResource"><see cref="PostgresServerResource"/> that this resource wraps.</param>
/// <param name="configureConstruct">Callback to configure construct.</param>
public class AzurePostgresResource(PostgresServerResource innerResource, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(innerResource.Name, configureConstruct),
    IResourceWithConnectionString,
    IPostgresResource
{
    /// <summary>
    /// Gets the "connectionString" secret output reference from the bicep template for the Azure Postgres Flexible Server.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection template for the manifest for the Azure Postgres Flexible Server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    /// <inheritdoc/>
    public override string Name => innerResource.Name;

    internal Dictionary<string, BicepSecretOutputReference> DatabaseConnectionStrings { get; } = [];

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => innerResource.Annotations;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="databaseName"></param>
    /// <returns></returns>
    public ReferenceExpression GetDatabaseConnectionString(string databaseName)
    {
        return ReferenceExpression.Create($"{DatabaseConnectionStrings[databaseName]}");
    }
}
