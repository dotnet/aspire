// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Postgres;

internal class PgAdminConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var adminResource = appModel.Resources.OfType<PgAdminContainerResource>().Single();
        var serverFileMount = adminResource.Annotations.OfType<VolumeMountAnnotation>().Single(v => v.Target == "/pgadmin4/servers.json");
        var postgresInstances = appModel.Resources.OfType<IPostgresParentResource>();

        var serverFileBuilder = new StringBuilder();

        using var stream = new FileStream(serverFileMount.Source, FileMode.Create);
        using var writer = new Utf8JsonWriter(stream);

        var serverIndex = 1;

        writer.WriteStartObject();
        writer.WriteStartObject("Servers");

        foreach (var postgresInstance in postgresInstances)
        {
            if (postgresInstance.TryGetAllocatedEndPoints(out var allocatedEndpoints))
            {
                var endpoint = allocatedEndpoints.Where(ae => ae.Name == "tcp").Single();

                var password = postgresInstance switch
                {
                    PostgresServerResource psr => psr.Password,
                    PostgresContainerResource pcr => pcr.Password,
                    _ => throw new InvalidOperationException("Postgres resource is neither PostgresServerResource or PostgresContainerResource.")
                };

                writer.WriteStartObject($"{serverIndex}");
                writer.WriteString("Name", postgresInstance.Name);
                writer.WriteString("Group", "Aspire instances");
                writer.WriteString("Host", "host.docker.internal");
                writer.WriteNumber("Port", endpoint.Port);
                writer.WriteString("Username", "postgres");
                writer.WriteString("SSLMode", "prefer");
                writer.WriteString("MaintenanceDB", "postgres");
                writer.WriteString("PasswordExecCommand", $"echo '{password}'"); // HACK: Generating a pass file and playing around with chmod is too painful.
                writer.WriteEndObject();
            }

            serverIndex++;
        }

        writer.WriteEndObject();
        writer.WriteEndObject();

        return Task.CompletedTask;
    }
}
