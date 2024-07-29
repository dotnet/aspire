// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Postgres;

internal sealed class PgWebConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var adminResource = appModel.Resources.OfType<PgWebContainerResource>().Single();
        var serverFileMount = adminResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == "/.pgweb/bookmarks");
        var postgresInstances = appModel.Resources.OfType<PostgresDatabaseResource>();

        var serverFileBuilder = new StringBuilder();

        foreach (var postgresDatabase in postgresInstances)
        {
            var user = postgresDatabase.Parent.UserNameParameter?.Value ?? "postgres";

            var fileContent = $"""
                host = "{postgresDatabase.Parent.PrimaryEndpoint.Host}"
                port = {postgresDatabase.Parent.PrimaryEndpoint.Port}
                user = "{user}"
                database = "{postgresDatabase.DatabaseName}"
                sslmode = "require"
                """;

            if (!Directory.Exists(serverFileMount.Source!))
            {
                Directory.CreateDirectory(serverFileMount.Source!);
            }
            var filePath = Path.Combine(serverFileMount.Source!, $"{postgresDatabase.Name}.toml");
            await File.WriteAllTextAsync(filePath, fileContent, cancellationToken).ConfigureAwait(false);
        }
    }
}
