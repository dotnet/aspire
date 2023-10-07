// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using System.Net.Sockets;

namespace Aspire.Hosting.Dcp;

public sealed class DcpDistributedApplicationLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        PrepareServices(appModel);

        return Task.CompletedTask;
    }

    private void PrepareServices(DistributedApplicationModel model)
    {
        // Automatically add ServiceBindingAnnotations to project components based on ApplicationUrl set in the launch profile.
        foreach (var projectComponent in model.Components.OfType<ProjectComponent>())
        {

            var selectedLaunchProfileName = projectComponent.SelectLaunchProfileName();
            if (selectedLaunchProfileName is null)
            {
                continue;
            }

            var launchSettings = projectComponent.GetLaunchSettings();
            var launchProfile = projectComponent.GetEffectiveLaunchProfile();
            if (launchProfile is null)
            {
                continue;
            }

            var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';') ?? [];
            foreach (var url in urlsFromApplicationUrl)
            {
                var uri = new Uri(url);

                if (projectComponent.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.Name == uri.Scheme))
                {
                    // If someone uses WithServiceBinding in the dev host to register a service binding with the name
                    // http or https this exception will be thrown.
                    throw new DistributedApplicationException($"Service binding annotation with name '{uri.Scheme}' already exists.");
                }

                var generatedServiceBindingAnnotation = new ServiceBindingAnnotation(
                    ProtocolType.Tcp,
                    uriScheme: uri.Scheme,
                    port: uri.Port
                    );

                projectComponent.Annotations.Add(generatedServiceBindingAnnotation);
            }
        }
    }
}
