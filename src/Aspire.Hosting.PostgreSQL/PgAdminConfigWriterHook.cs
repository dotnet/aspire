// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Postgres;

internal sealed class PgAdminConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var adminResource = appModel.Resources.OfType<PgAdminContainerResource>().Single();
        var serverFileMount = adminResource.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == "/pgadmin4/servers.json");
        var postgresInstances = appModel.Resources.OfType<PostgresServerResource>();

        var serverFileBuilder = new StringBuilder();

        using var stream = new FileStream(serverFileMount.Source!, FileMode.Create);
        using var writer = new Utf8JsonWriter(stream);

        var serverIndex = 1;

        writer.WriteStartObject();
        writer.WriteStartObject("Servers");

        foreach (var postgresInstance in postgresInstances)
        {
            if (postgresInstance.PrimaryEndpoint.IsAllocated)
            {
                var endpoint = postgresInstance.PrimaryEndpoint;

                writer.WriteStartObject($"{serverIndex}");
                writer.WriteString("Name", postgresInstance.Name);
                writer.WriteString("Group", "Aspire instances");
                writer.WriteString("Host", endpoint.ContainerHost);
                writer.WriteNumber("Port", endpoint.Port);
                writer.WriteString("Username", "postgres");
                writer.WriteString("SSLMode", "prefer");
                writer.WriteString("MaintenanceDB", "postgres");
                writer.WriteString("PasswordExecCommand", $"echo '{postgresInstance.Password}'"); // HACK: Generating a pass file and playing around with chmod is too painful.
                writer.WriteEndObject();
            }

            serverIndex++;
        }

        writer.WriteEndObject();
        writer.WriteEndObject();

        return Task.CompletedTask;
    }
}
