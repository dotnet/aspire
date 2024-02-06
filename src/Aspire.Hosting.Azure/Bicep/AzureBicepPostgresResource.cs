// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an resource for Azure Postgres Flexible Server.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="username">A delegate to resolve the username.</param>
/// <param name="password">A delegate to resolve the password.</param>
public class AzureBicepPostgresResource(string name, Func<string> username, Func<string> password) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.postgres.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Databases { get; } = [];

    /// <summary>
    /// Gets the connection string for the Azure Postgres Flexible Server.
    /// </summary>
    /// <returns>The connection string.</returns>
    public string? GetConnectionString()
    {
        return $"Host={Outputs["pgfqdn"]};Username={username()};Password={password()};";
    }
}

/// <summary>
/// Represents a resource for an Azure Postgres Flexible Server database.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name</param>
/// <param name="parent">The <see cref="AzureBicepPostgresResource"/> that this database is a part of.</param>
public class AzureBicepPostgresDbResource(string name, string databaseName, AzureBicepPostgresResource parent) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepPostgresResource>
{
    public AzureBicepPostgresResource Parent { get; } = parent;

    public string? GetConnectionString()
    {
        return $"{Parent.GetConnectionString()};Database={databaseName}";
    }

    public void WriteToManifest(ManifestPublishingContext context)
    {
        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.connectionString}};Database={databaseName}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

/// <summary>
/// Provides extension methods for adding the Azure Postgres resources to the application model.
/// </summary>
public static class AzureBicepPostgresExtensions
{
    /// <summary>
    /// Adds an Azure Postgres resource to the application model. This resource can be used to create Azure Postgres Flexible Server resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="administratorLogin">The administrator login.</param>
    /// <param name="administratorLoginPassword">The administrator password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepPostgresResource> AddBicepAzurePostgres(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> administratorLogin,
        IResourceBuilder<ParameterResource> administratorLoginPassword)
    {
        var resource = new AzureBicepPostgresResource(name, () => administratorLogin.Resource.Value, () => administratorLoginPassword.Resource.Value)
        {
            ConnectionStringTemplate = $"Host={{{name}.outputs.pgfqdn}};Username={{{administratorLogin.Resource.Name}.value}};Password={{{administratorLoginPassword.Resource.Name}.value}}"
        };

        return builder.AddResource(resource)
            .WithParameter("serverName", resource.CreateBicepResourceName())
            .WithParameter("administratorLogin", administratorLogin)
            .WithParameter("administratorLoginPassword", administratorLoginPassword)
            .WithParameter("databases", resource.Databases)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Postgres resource to the application model. This resource can be used to create Azure Postgres Flexible Server resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="administratorLogin">The administrator login.</param>
    /// <param name="administratorLoginPassword">The administrator password.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepPostgresResource> AddBicepAzurePostgres(this IDistributedApplicationBuilder builder,
        string name,
        string administratorLogin,
        IResourceBuilder<ParameterResource> administratorLoginPassword)
    {
        var resource = new AzureBicepPostgresResource(name, () => administratorLogin, () => administratorLoginPassword.Resource.Value)
        {
            ConnectionStringTemplate = $"Host={{{name}.outputs.pgfqdn}};Username={administratorLogin};Password={{{administratorLoginPassword.Resource.Name}.value}}"
        };

        return builder.AddResource(resource)
            .WithParameter("serverName", resource.CreateBicepResourceName())
            .WithParameter("administratorLogin", administratorLogin)
            .WithParameter("administratorLoginPassword", administratorLoginPassword)
            .WithParameter("databases", resource.Databases)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Postgres database to the application model.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the database resource.</param>
    /// <param name="databaseName">The name of the database.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureBicepPostgresDbResource> AddDatabase(this IResourceBuilder<AzureBicepPostgresResource> builder, string name, string? databaseName = null)
    {
        var resource = new AzureBicepPostgresDbResource(name, databaseName ?? name, builder.Resource);

        builder.Resource.Databases.Add(databaseName ?? name);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
