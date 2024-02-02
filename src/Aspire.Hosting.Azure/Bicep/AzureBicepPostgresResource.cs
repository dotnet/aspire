// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepPostgresResource(string name, Func<string> username, Func<string> password) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.postgres.bicep"),
    IResourceWithConnectionString
{
    internal List<string> Databases { get; } = [];

    public string? GetConnectionString()
    {
        return $"Host={Outputs["pgfqdn"]};Username={username()};Password={password()};";
    }
}

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
        var resource = this;

        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{resource.Parent.Name}.connectionString}};Database={databaseName}");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }
}

public static class AzureBicepPostgresExtensions
{
    public static IResourceBuilder<AzureBicepPostgresResource> AddAzurePostgres(this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> administratorLogin,
        IResourceBuilder<ParameterResource> administratorLoginPassword)
    {
        var resource = new AzureBicepPostgresResource(name, () => administratorLogin.Resource.Value, () => administratorLoginPassword.Resource.Value)
        {
            ConnectionStringTemplate = $"Host={{{name}.outputs.pgfqdn}};Username={{{administratorLogin.Resource.Name}.value}};Password={{{administratorLoginPassword.Resource.Name}.value}}"
        };

        return builder.AddResource(resource)
            .AddParameter("serverName", resource.CreateBicepResourceName())
            .AddParameter("administratorLogin", administratorLogin)
            .AddParameter("administratorLoginPassword", administratorLoginPassword)
            .AddParameter("databases", resource.Databases)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepPostgresResource> AddAzurePostgres(this IDistributedApplicationBuilder builder,
        string name,
        string administratorLogin,
        IResourceBuilder<ParameterResource> administratorLoginPassword)
    {
        var resource = new AzureBicepPostgresResource(name, () => administratorLogin, () => administratorLoginPassword.Resource.Value)
        {
            ConnectionStringTemplate = $"Host={{{name}.outputs.pgfqdn}};Username={administratorLogin};Password={{{administratorLoginPassword.Resource.Name}.value}}"
        };

        return builder.AddResource(resource)
            .AddParameter("serverName", resource.CreateBicepResourceName())
            .AddParameter("administratorLogin", administratorLogin)
            .AddParameter("administratorLoginPassword", administratorLoginPassword)
            .AddParameter("databases", resource.Databases)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepPostgresDbResource> AddDatabase(this IResourceBuilder<AzureBicepPostgresResource> builder, string name, string? databaseName = null)
    {
        var resource = new AzureBicepPostgresDbResource(name, databaseName ?? name, builder.Resource);

        builder.Resource.Databases.Add(databaseName ?? name);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
