// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Kusto.Data;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Represents an Azure Kusto database resource, which is a child resource of a <see cref="AzureKustoClusterResource"/>.
/// </summary>
public class AzureKustoDatabaseResource : Resource, IResourceWithParent<AzureKustoClusterResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKustoDatabaseResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="kustoParentResource">The Kusto parent resource associated with this database.</param>
    public AzureKustoDatabaseResource(string name, string databaseName, AzureKustoClusterResource kustoParentResource)
        : base(name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(kustoParentResource);

        DatabaseName = databaseName;
        Parent = kustoParentResource;
    }

    /// <summary>
    /// Gets the parent Kusto resource.
    /// </summary>
    public AzureKustoClusterResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Kusto database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var connectionStringBuilder = new KustoConnectionStringBuilder
            {
                InitialCatalog = DatabaseName
            };

            return ReferenceExpression.Create($"{Parent};{connectionStringBuilder.ToString()}");
        }
    }

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; }
}
