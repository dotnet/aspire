// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Sql Server resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepSqlServerResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.sql.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Databases { get; } = [];

    /// <summary>
    /// Gets the connection template for the manifest for the Azure SQL Server resource.
    /// </summary>
    public string ConnectionStringExpression =>
        $"Server=tcp:{{{Name}.outputs.sqlServerFqdn}},1433;Encrypt=True;Authentication=\"Active Directory Default\"";

    /// <summary>
    /// Gets the connection string for the Azure SQL Server resource.
    /// </summary>
    /// <returns>The connection string for the Azure SQL Server resource.</returns>
    public string? GetConnectionString()
    {
        return $"Server=tcp:{Outputs["sqlServerFqdn"]},1433;Encrypt=True;Authentication=\"Active Directory Default\"";
    }
}

/// <summary>
/// Represents an Azure SQL Database resource.
/// </summary>
public class AzureBicepSqlDbResource(string name, string databaseName, AzureBicepSqlServerResource parent) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepSqlServerResource>
{
    /// <summary>
    /// Gets the parent Azure SQL Server resource.
    /// </summary>
    public AzureBicepSqlServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection template for the manifest for the Azure SQL Database resource.
    /// </summary>
    public string ConnectionStringExpression =>
        $"{{{Parent.Name}.connectionString}};Intial Catalog={databaseName}";

    /// <summary>
    /// Gets the connection string for the Azure SQL Database resource.
    /// </summary>
    /// <returns>The connection string for the Azure SQL Database resource.</returns>
    public string? GetConnectionString()
    {
        return $"{Parent.GetConnectionString()};Initial Catalog={databaseName}";
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", ConnectionStringExpression);
        context.Writer.WriteString("parent", Parent.Name);
    }
}

/// <summary>
/// Provides extension methods for adding the Azure SQL resources to the application model.
/// </summary>
public static class AzureBicepSqlExtensions
{
    /// <summary>
    /// Adds an Azure SQL Server resource to the application model. This resource can be used to create Azure SQL Database resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepSqlServerResource> AddBicepAzureSqlServer(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepSqlServerResource(name);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
                      .WithParameter("serverName", resource.CreateBicepResourceName())
                      .WithParameter("databases", resource.Databases)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure SQL Database resource to the application model. This resource requires an <see cref="AzureSqlServerResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepSqlDbResource> AddDatabase(this IResourceBuilder<AzureBicepSqlServerResource> builder, string name, string? databaseName = null)
    {
        var dbName = databaseName ?? name;
        var resource = new AzureBicepSqlDbResource(name, dbName, builder.Resource);

        builder.Resource.Databases.Add(dbName);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
