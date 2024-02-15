// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.MySql;

internal class PhpMyAdminConfigWriterHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var adminResource = appModel.Resources.OfType<PhpMyAdminContainerResource>().Single();
        var serverFileMount = adminResource.Annotations.OfType<VolumeMountAnnotation>().Single(v => v.Target == "/etc/phpmyadmin/config.user.inc.php");
        var mySqlInstances = appModel.Resources.OfType<MySqlServerResource>();

        if (appModel.Resources.OfType<PhpMyAdminContainerResource>().SingleOrDefault() is not { } myAdminResource)
        {
            // No-op if there is no myAdmin resource (removed after hook added).
            return Task.CompletedTask;
        }

        if (!mySqlInstances.Any())
        {
            // No-op if there are no MySql resources present.
            return Task.CompletedTask;
        }

        if (mySqlInstances.Count() == 1)
        {
            var singleInstance = mySqlInstances.Single();
            if (singleInstance.TryGetAllocatedEndPoints(out var allocatedEndPoints))
            {
                var endpoint = allocatedEndPoints.Where(ae => ae.Name == "tcp").Single();
                myAdminResource.Annotations.Add(new EnvironmentCallbackAnnotation((EnvironmentCallbackContext context) =>
                {
                    context.EnvironmentVariables.Add("PMA_HOST", $"host.docker.internal:{endpoint.Port}");
                    context.EnvironmentVariables.Add("PMA_USER", "root");
                    context.EnvironmentVariables.Add("PMA_PASSWORD", singleInstance.Password);
                }));
            }
        }
        else
        {
            using var stream = new FileStream(serverFileMount.Source, FileMode.Create);
            using var writer = new StreamWriter(stream);

            writer.WriteLine("<?php");
            writer.WriteLine();
            writer.WriteLine("$i = 0;");
            writer.WriteLine();
            foreach (var mySqlInstance in mySqlInstances)
            {
                if (mySqlInstance.TryGetAllocatedEndPoints(out var allocatedEndpoints))
                {
                    var endpoint = allocatedEndpoints.Where(ae => ae.Name == "tcp").Single();
                    writer.WriteLine("$i++;");
                    writer.WriteLine($"$cfg['Servers'][$i]['host'] = 'host.docker.internal:{endpoint.Port}';");
                    writer.WriteLine($"$cfg['Servers'][$i]['verbose'] = '{mySqlInstance.Name}';");
                    writer.WriteLine($"$cfg['Servers'][$i]['auth_type'] = 'cookie';");
                    writer.WriteLine($"$cfg['Servers'][$i]['user'] = 'root';");
                    writer.WriteLine($"$cfg['Servers'][$i]['password'] = '{mySqlInstance.Password}';");
                    writer.WriteLine($"$cfg['Servers'][$i]['AllowNoPassword'] = true;");
                    writer.WriteLine();
                }
            }
            writer.WriteLine("$cfg['DefaultServer'] = 1;");
            writer.WriteLine("?>");
        }

        return Task.CompletedTask;
    }
}
