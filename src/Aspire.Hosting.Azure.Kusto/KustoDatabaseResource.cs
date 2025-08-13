// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Kusto.Data;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Represents a Kusto database resource, which is a child resource of a <see cref="KustoResource"/>.
/// </summary>
public class KustoDatabaseResource : Resource, IResourceWithParent<KustoResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KustoDatabaseResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="databaseName">The database name.</param>
    /// <param name="kustoParentResource">The Kusto parent resource associated with this database.</param>
    public KustoDatabaseResource(string name, string databaseName, KustoResource kustoParentResource)
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
    public KustoResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Postgres database.
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
