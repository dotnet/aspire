// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Sql;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Contains extension methods for enabling Aspire's Redis resource to be deployed to Azure.
/// </summary>
public static class AzureSqlDatabaseResourceBuilderExtensions
{
    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="builder">TODO:</param>
    /// <param name="configureResource">TODO:</param>
    /// <returns>TODO:</returns>
    public static IResourceBuilder<SqlServerServerResource> PublishAsAzureSqlDatabase(this IResourceBuilder<SqlServerServerResource> builder, Action<BicepGenerationContext>? configureResource = null)
    {
        return builder.WithManifestPublishingCallback(context => ProvisioningExtensions.WriteBicepResourceToManifest(context, builder.Resource))
                      .WithBicepGenerationCallback(configureResource ?? GenerateDefaultSqlServerResource);
    }

    private static void GenerateDefaultSqlServerResource(BicepGenerationContext context)
    {
        var sql1 = new SqlServer(context, context.Resource.Name + "1");
        var sql2 = new SqlServer(context, context.Resource.Name + "2");
        var childDatabases = context.AppModel.Resources.OfType<SqlServerDatabaseResource>().Where(r => r.Parent == context.Resource);

        foreach (var childDatabase in childDatabases)
        {
            var db = new SqlDatabase(context, childDatabase.Name);
        }
    }
}
