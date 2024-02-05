// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepSqlServerResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.sql.bicep"),
    IResourceWithConnectionString
{
    public List<string> Databases { get; } = [];

    public string? GetConnectionString()
    {
        return $"Server=tcp:{Outputs["sqlServerFqdn"]},1433;Encrypt=True;Authentication=\"Active Directory Default\"";
    }
}

public class AzureBicepSqlDbResource(string name, string databaseName, AzureBicepSqlServerResource parent) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepSqlServerResource>
{
    public AzureBicepSqlServerResource Parent { get; } = parent;

    public string? GetConnectionString()
    {
        return $"{Parent.GetConnectionString()};Initial Catalog={databaseName}";
    }

    public void WriteToManifest(ManifestPublishingContext context)
    {
        var resource = this;

        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{resource.Parent.Name}.connectionString}};Intial Catalog={databaseName}");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }
}

public static class AzureBicepSqlExtensions
{
    public static IResourceBuilder<AzureBicepSqlServerResource> AddBicepAzureSql(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepSqlServerResource(name)
        {
            ConnectionStringTemplate = $"Server=tcp:{{{name}.outputs.sqlServerFqdn}},1433;Encrypt=True;Authentication=\"Active Directory Default\""
        };

        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
                      .WithParameter("serverName", resource.CreateBicepResourceName())
                      .WithParameter("databases", resource.Databases)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepSqlDbResource> AddDatabase(this IResourceBuilder<AzureBicepSqlServerResource> builder, string name, string? databaseName = null)
    {
        var resource = new AzureBicepSqlDbResource(name, databaseName ?? name, builder.Resource);

        builder.Resource.Databases.Add(databaseName ?? name);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
