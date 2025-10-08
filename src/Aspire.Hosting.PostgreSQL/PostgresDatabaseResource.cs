// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Aspire.Hosting.ApplicationModel.PostgresDatabaseResource;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL database. This is a child resource of a <see cref="PostgresServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="postgresParentResource">The PostgreSQL parent resource associated with this database.</param>
public class PostgresDatabaseResource(string name, string databaseName, PostgresServerResource postgresParentResource)
    : Resource(name), IResourceWithParent<PostgresServerResource>, IResourceWithConnectionString, IResourceWithProperties<Properties>
{
    /// <summary>
    /// Gets the parent PostgresSQL container resource.
    /// </summary>
    public PostgresServerResource Parent { get; } = postgresParentResource ?? throw new ArgumentNullException(nameof(postgresParentResource));

    /// <summary>
    /// Gets the connection string expression for the Postgres database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var connectionStringBuilder = new DbConnectionStringBuilder
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

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }

    /// <inheritdoc />
    public ReferenceExpression GetProperty(Properties key) => key switch
    {
        Properties.Host => ReferenceExpression.Create($"{Parent.PrimaryEndpoint.Property(EndpointProperty.Host)}"),
        Properties.Port => ReferenceExpression.Create($"{Parent.PrimaryEndpoint.Property(EndpointProperty.Port)}"),
        Properties.Username => ReferenceExpression.Create($"{Parent.UserNameReference}"),
        Properties.Password => ReferenceExpression.Create($"{Parent.PasswordParameter}"),
        Properties.Database => ReferenceExpression.Create($"{DatabaseName}"),
        _ => throw new ArgumentException($"Unknown property: {key}", nameof(key))
    };

    /// <summary>
    /// Properties
    /// </summary>
    public enum Properties
    {
        /// <summary>
        /// Host
        /// </summary>
        Host,

        /// <summary>
        /// Port
        /// </summary>
        Port,

        /// <summary>
        /// Username
        /// </summary>
        Username,

        /// <summary>
        /// Password
        /// </summary>
        Password,

        /// <summary>
        /// Database
        /// </summary>
        Database
    }
}

