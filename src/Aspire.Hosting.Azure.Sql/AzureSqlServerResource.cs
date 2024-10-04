// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Sql Server resource.
/// </summary>
public class AzureSqlServerResource : AzureConstructResource, IResourceWithConnectionString
{
    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);
    private readonly bool _useInnerResourceAnnotations;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
    public AzureSqlServerResource(string name, Action<ResourceModuleConstruct> configureConstruct)
        : base(name, configureConstruct) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlServerResource"/> class.
    /// </summary>
    /// <param name="innerResource">The <see cref="SqlServerServerResource"/> that this resource wraps.</param>
    /// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
    [Obsolete($"This method is obsolete and will be removed in a future version. Use {nameof(AzureSqlExtensions.AddAzureSqlServer)} instead to add an Azure SQL server resource.")]
    public AzureSqlServerResource(SqlServerServerResource innerResource, Action<ResourceModuleConstruct> configureConstruct)
        : base(innerResource.Name, configureConstruct)
    {
        InnerResource = innerResource;
        _useInnerResourceAnnotations = true;
    }

    /// <summary>
    /// Gets the fully qualified domain name (FQDN) output reference from the bicep template for the Azure SQL Server resource.
    /// </summary>
    public BicepOutputReference FullyQualifiedDomainName => new("sqlServerFqdn", this);

    /// <summary>
    /// Gets the connection template for the manifest for the Azure SQL Server resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        InnerResource?.ConnectionStringExpression ??
            ReferenceExpression.Create(
                $"Server=tcp:{FullyQualifiedDomainName},1433;Encrypt=True;Authentication=\"Active Directory Default\"");

    /// <inheritdoc />
    public override ResourceAnnotationCollection Annotations => _useInnerResourceAnnotations ? InnerResource!.Annotations : base.Annotations;

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }

    internal SqlServerServerResource? InnerResource { get; set; }
}
