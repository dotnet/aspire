// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.ArgumentExceptionExtensions;
using MySqlConnector;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL database. This is a child resource of a <see cref="MySqlServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The MySQL parent resource associated with this database.</param>
public class MySqlDatabaseResource(string name, string databaseName, MySqlServerResource parent)
    : Resource(name), IResourceWithParent<MySqlServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent MySQL container resource.
    /// </summary>
    public MySqlServerResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the MySQL database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder
            {
                ["Database"] = DatabaseName
            };

            return ReferenceExpression.Create($"{Parent};{connectionStringBuilder.ToString()}");
        }
    }
    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);
}
