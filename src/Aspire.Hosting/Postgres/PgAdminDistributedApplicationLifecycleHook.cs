// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Postgres;
internal class PgAdminDistributedApplicationLifecycleHook(int hostPort, PgAdminOptions options) : IDistributedApplicationLifecycleHook
{
    private readonly int _hostPort = hostPort;
    private readonly PgAdminOptions _options = options;

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var postgresResources = appModel.Resources.OfType<IPostgresResource>();
        if (!postgresResources.Any())
        {
            return Task.CompletedTask;
        }

        var pgAdminContainer = new ContainerResource("pgadmin");
        pgAdminContainer.Annotations.Add(new ContainerImageAnnotation() { Image = "dpage/pgadmin4", Tag = "latest" }); 
        pgAdminContainer.Annotations.Add(new ServiceBindingAnnotation(System.Net.Sockets.ProtocolType.Tcp, port: _hostPort, containerPort: 80, uriScheme: "http", name: "pgadmin"));
        pgAdminContainer.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context["PGADMIN_DEFAULT_EMAIL"] = _options.DefaultEmail;
            context["PGADMIN_DEFAULT_PASSWORD"] = _options.DefaultPassword;
        }));

        appModel.Resources.Add(pgAdminContainer);

        return Task.CompletedTask;
    }
}
