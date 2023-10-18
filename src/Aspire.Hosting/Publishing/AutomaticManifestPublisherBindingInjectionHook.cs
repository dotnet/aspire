// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;
internal sealed class AutomaticManifestPublisherBindingInjectionHook(IOptions<PublishingOptions> publishingOptions) : IDistributedApplicationLifecycleHook
{
    private readonly IOptions<PublishingOptions> _publishingOptions = publishingOptions;

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (_publishingOptions.Value.Publisher != "manifest")
        {
            return Task.CompletedTask;
        }

        var projectResources = appModel.Resources.OfType<ProjectResource>();

        foreach (var projectResource in projectResources)
        {
            // TODO: Add logic here that analyzes each project and figures out the best
            //       bindings to automatically add.
            if (!projectResource.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.UriScheme == "http" || sb.Name == "http"))
            {
                var httpBinding = new ServiceBindingAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "http"
                    );
                projectResource.Annotations.Add(httpBinding);
            }

            if (!projectResource.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.UriScheme == "https" || sb.Name == "https"))
            {
                var httpsBinding = new ServiceBindingAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "https"
                    );
                projectResource.Annotations.Add(httpsBinding);
            }
        }

        return Task.CompletedTask;
    }
}
