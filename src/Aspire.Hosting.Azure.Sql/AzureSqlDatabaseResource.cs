// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure SQL database. This is a child resource of an <see cref="AzureSqlServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The Azure SQL Database (server) parent resource associated with this database.</param>
public class AzureSqlDatabaseResource(string name, string databaseName, AzureSqlServerResource parent)
    : Resource(ThrowIfNull(name)), IResourceWithParent<AzureSqlServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Azure SQL Database (server) resource.
    /// </summary>
    public AzureSqlServerResource Parent { get; } = ThrowIfNull(parent);

    /// <summary>
    /// Gets the connection string expression for the Azure SQL database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNull(databaseName);

    /// <summary>
    /// Gets the inner SqlServerDatabaseResource resource.
    /// 
    /// This is set when RunAsContainer is called on the AzureSqlServerResource resource to create a local SQL Server container.
    /// </summary>
    internal SqlServerDatabaseResource? InnerResource { get; private set; }

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => InnerResource?.Annotations ?? base.Annotations;

    private static T ThrowIfNull<T>([NotNull] T? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => argument ?? throw new ArgumentNullException(paramName);

    internal void SetInnerResource(SqlServerDatabaseResource innerResource)
    {
        // Copy the annotations to the inner resource before making it the inner resource
        foreach (var annotation in Annotations)
        {
            innerResource.Annotations.Add(annotation);
        }

        InnerResource = innerResource;
    }

}
