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
    : Resource(name), IResourceWithParent<AzureSqlServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Free Azure SQL database offer
    /// </summary>
    internal const string FREE_SKU_NAME = "Free";

    /// <summary>
    /// SKU associated with the free offer
    /// </summary>
    internal const string FREE_DB_SKU = "GP_S_Gen5_2";

    private string _skuName = FREE_SKU_NAME;

    /// <summary>
    /// Gets the parent Azure SQL Database (server) resource.
    /// </summary>
    public AzureSqlServerResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the Azure SQL database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);

    /// <summary>
    /// Gets or Sets the database SKU name
    /// </summary>
    public string SkuName
    {
        get { return _skuName; }
        internal set { ThrowIfNullOrEmpty(value); _skuName = value.Trim(); }
    }

    /// <summary>
    /// Gets the inner SqlServerDatabaseResource resource.
    /// 
    /// This is set when RunAsContainer is called on the AzureSqlServerResource resource to create a local SQL Server container.
    /// </summary>
    internal SqlServerDatabaseResource? InnerResource { get; private set; }

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => InnerResource?.Annotations ?? base.Annotations;

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }

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
