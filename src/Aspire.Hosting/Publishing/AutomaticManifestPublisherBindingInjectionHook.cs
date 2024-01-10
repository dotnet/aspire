// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;
internal sealed class AutomaticManifestPublisherBindingInjectionHook(IOptions<PublishingOptions> publishingOptions) : IDistributedApplicationLifecycleHook
{
    private readonly IOptions<PublishingOptions> _publishingOptions = publishingOptions;

    private static bool IsKestrelHttp2ConfigurationPresent(ProjectResource projectResource)
    {
        var serviceMetadata = projectResource.GetServiceMetadata();

        var projectDirectoryPath = Path.GetDirectoryName(serviceMetadata.ProjectPath)!;
        var appSettingsPath = Path.Combine(projectDirectoryPath, "appsettings.json");
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var appSettingsEnvironmentPath = Path.Combine(projectDirectoryPath, $"appsettings.{env}.json");

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile(appSettingsPath, optional: true);
        configBuilder.AddJsonFile(appSettingsEnvironmentPath, optional: true);
        var config = configBuilder.Build();
        var protocol = config["Kestrel:EndpointDefaults:Protocols"];
        return protocol == "Http2";
    }

    private static bool IsWebProject(ProjectResource projectResource)
    {
        var launchProfile = projectResource.GetEffectiveLaunchProfile();
        return launchProfile?.ApplicationUrl != null;
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
            var isHttp2ConfiguredInAppSettings = IsKestrelHttp2ConfigurationPresent(projectResource);

            // If we aren't a web project we don't automatically add bindings.
            if (!IsWebProject(projectResource))
            {
                continue;
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "http" || sb.Name == "http"))
            {
                var httpBinding = new EndpointAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "http"
                    );
                projectResource.Annotations.Add(httpBinding);
                httpBinding.Transport = isHttp2ConfiguredInAppSettings ? "http2" : httpBinding.Transport;
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "https" || sb.Name == "https"))
            {
                var httpsBinding = new EndpointAnnotation(
                    System.Net.Sockets.ProtocolType.Tcp,
                    uriScheme: "https"
                    );
                projectResource.Annotations.Add(httpsBinding);
                httpsBinding.Transport = isHttp2ConfiguredInAppSettings ? "http2" : httpsBinding.Transport;
            }
        }

        return Task.CompletedTask;
    }
}
