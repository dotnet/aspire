// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;
internal sealed class AutomaticManifestPublisherBindingInjectionHook(IOptions<PublishingOptions> publishingOptions) : IDistributedApplicationLifecycleHook
{
    private readonly IOptions<PublishingOptions> _publishingOptions = publishingOptions;

    private static bool IsKestrelHttp2ConfigurationPresent(ProjectResource projectResource)
    {
        var serviceMetadata = projectResource.GetServiceMetadata();
        var projectDirectoryPath = Path.GetDirectoryName(serviceMetadata.ProjectPath);
        var appSettingsPath = Path.Combine(projectDirectoryPath!, "appsettings.json");
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile(appSettingsPath);
        var config = configBuilder.Build();
        var protocol = config.GetValue<string>("Kestrel:EndpointDefaults:Protocols");
        return protocol == "Http2";
    }

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (_publishingOptions.Value.Publisher != "manifest")
        {
            return Task.CompletedTask;
        }

        var projectResources = appModel.Resources.OfType<ProjectResource>();

        foreach (var projectResource in projectResources)
        {
            ConfigurationBuilder b = new ConfigurationBuilder();

            var projectMetadata = projectResource.GetServiceMetadata();
            var projectPath = projectMetadata.ProjectPath;
            var projectDirectory = Path.GetDirectoryName(projectPath);

            var configSource = new JsonConfigurationSource();
            configSource.Path = Path.Combine(projectDirectory!, "appsettings.json");
            var provider = configSource.Build(b);

            // TODO: Add logic here that analyzes each project and figures out the best
            //       bindings to automatically add.
            if (!projectResource.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.UriScheme == "http" || sb.Name == "http"))
            {
                var httpBinding = new ServiceBindingAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "http"
                    );
                projectResource.Annotations.Add(httpBinding);
                httpBinding.Transport = IsKestrelHttp2ConfigurationPresent(projectResource) ? "http2" : httpBinding.Transport;
            }

            if (!projectResource.Annotations.OfType<ServiceBindingAnnotation>().Any(sb => sb.UriScheme == "https" || sb.Name == "https"))
            {
                var httpsBinding = new ServiceBindingAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "https"
                    );
                projectResource.Annotations.Add(httpsBinding);
                httpsBinding.Transport = IsKestrelHttp2ConfigurationPresent(projectResource) ? "http2" : httpsBinding.Transport;
            }
        }

        return Task.CompletedTask;
    }
}
