// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace Aspire.Hosting.Dcp;

public sealed class DcpDistributedApplicationLifecycleHook(IOptions<PublishingOptions> publishingOptions) : IDistributedApplicationLifecycleHook
{
    private readonly IOptions<PublishingOptions> _publishingOptions = publishingOptions;

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var publisher = _publishingOptions.Value?.Publisher == null ? "dcp" : _publishingOptions.Value.Publisher;

        if (publisher == "dcp")
        {
            PrepareServices(appModel);
        }

        return Task.CompletedTask;
    }

    private void PrepareServices(DistributedApplicationModel model)
    {
        // Automatically add ServiceBindingAnnotations to project resources based on ApplicationUrl set in the launch profile.
        foreach (var projectResource in model.Resources.OfType<ProjectResource>())
        {
            var selectedLaunchProfileName = projectResource.SelectLaunchProfileName();
            if (selectedLaunchProfileName is null)
            {
                continue;
            }

            var launchProfile = projectResource.GetEffectiveLaunchProfile();
            if (launchProfile is null)
            {
                continue;
            }

            var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';') ?? [];
            foreach (var url in urlsFromApplicationUrl)
            {
                var uri = new Uri(url);

                if (projectResource.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.Name == uri.Scheme))
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

                projectResource.Annotations.Add(generatedServiceBindingAnnotation);
            }
        }
    }
}
