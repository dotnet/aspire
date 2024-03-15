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
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" secret output reference from the bicep template for the Azure Postgres Flexible Server.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection template for the manifest for the Azure Postgres Flexible Server.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure Postgres Flexible Server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ConnectionString.GetValueAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override string Name => innerResource.Name;

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => innerResource.Annotations;
}
