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

        // HACK: We only automatically add service bindings when publishing under DCP, but this lifecycle hook
        //       always runs because we use it to deterministically sequence logic in the lifecycle.
        if (publisher == "dcp")
        {
            PrepareServices(appModel);
        }

        foreach (var resource in appModel.Resources)
        {
            // Grab the service bindings we already have for this resource.
            var serviceBindingsLookup = resource.Annotations
                .OfType<ServiceBindingAnnotation>()
                .ToLookup(a => a.Name);

            // Find any callbacks for this specific publisher.
            var bindingNameGroupedCallbackAnnotations = resource.Annotations
                .OfType<ServiceBindingCallbackAnnotation>()
                .Where(a => a.PublisherName == publisher)
                .ToLookup(a => a.BindingName);

            foreach (var callbackAnnotationsForBinding in bindingNameGroupedCallbackAnnotations)
            {
                // For each callback that maps to this publisher and service binding name, find
                // the existing service binding annotation, and if it doesn't exist create one.
                ServiceBindingAnnotation inputAnnotation = serviceBindingsLookup.Contains(callbackAnnotationsForBinding.Key)
                    ? serviceBindingsLookup[callbackAnnotationsForBinding.Key].Single()
                    : CreateServiceBindingAnnotation(callbackAnnotationsForBinding.Key);

                var callbackAnnotation = callbackAnnotationsForBinding.Single(); // Initially will only support one callback annotation per binding???
                ServiceBindingAnnotation outputAnnotation = inputAnnotation;

                // If the callback exists, invoke it (sometimes it won't exist if someone is
                // just using the WithServiceBindingForPublisher(...) to bring a service binding into
                // existence.
                if (callbackAnnotation.Callback != null)
                {
                    var context = new ServiceBindingCallbackContext(publisher, inputAnnotation);
                    outputAnnotation = callbackAnnotation.Callback(context);
                }

                // Currently we support swapping out the existing service binding for a completely
                // new one. This is done to enable some interesting scenarios in the future around
                // transparently adding reverse proxies/tunnels.
                if (resource.Annotations.Contains(inputAnnotation))
                {
                    resource.Annotations.Remove(inputAnnotation);
                }

                if (!resource.Annotations.Contains(outputAnnotation))
                {
                    resource.Annotations.Add(outputAnnotation);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static ServiceBindingAnnotation CreateServiceBindingAnnotation(string bindingName)
    {
        return bindingName.ToLowerInvariant() switch
        {
            "http" => new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "http", containerPort: 80),
            "https" => new ServiceBindingAnnotation(ProtocolType.Tcp, uriScheme: "https", containerPort: 443),
            _ => new ServiceBindingAnnotation(ProtocolType.Tcp, name: bindingName)
        };
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
