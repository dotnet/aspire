// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Docker Compose file resources to the application model.
/// </summary>
public static class DockerComposeFileResourceBuilderExtensions
{
    /// <summary>
    /// Adds a Docker Compose file to the application model by parsing the compose file and creating container resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="composeFilePath">The path to the docker-compose.yml file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DockerComposeFileResource}"/>.</returns>
    /// <remarks>
    /// This method parses the docker-compose.yml file and translates supported services into Aspire container resources.
    /// Services that cannot be translated are skipped with a warning.
    /// All created resources are children of the DockerComposeFileResource.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddDockerComposeFile("mycompose", "./docker-compose.yml");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<DockerComposeFileResource> AddDockerComposeFile(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string composeFilePath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(composeFilePath);

        var resource = new DockerComposeFileResource(name, composeFilePath);
        
        // Parse and import the compose file synchronously to add resources to the model
        // Capture any exceptions to report during initialization
        Exception? parseException = null;
        List<string> warnings = [];
        
        try
        {
            ParseAndImportComposeFile(builder, resource, composeFilePath, warnings);
        }
        catch (Exception ex)
        {
            parseException = ex;
        }
        
        // Use OnInitializeResource to report any issues that occurred during parsing
        return builder.AddResource(resource).ExcludeFromManifest().OnInitializeResource(async (resource, e, ct) =>
        {
            if (parseException is not null)
            {
                e.Logger.LogError(parseException, "Failed to parse Docker Compose file: {ComposeFilePath}", composeFilePath);
                await e.Notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.FailedToStart }).ConfigureAwait(false);
            }
            else if (warnings.Count > 0)
            {
                foreach (var warning in warnings)
                {
                    e.Logger.LogWarning("{Warning}", warning);
                }
            }
            await e.Notifications.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Running }).ConfigureAwait(false);
        });
    }

    private static void ParseAndImportComposeFile(
        IDistributedApplicationBuilder builder,
        DockerComposeFileResource parentResource,
        string composeFilePath,
        List<string> warnings)
    {
        if (!File.Exists(composeFilePath))
        {
            throw new FileNotFoundException($"Docker Compose file not found: {composeFilePath}", composeFilePath);
        }

        var yamlContent = File.ReadAllText(composeFilePath);
        
        ComposeFile composeFile;
        try
        {
            composeFile = ComposeFile.FromYaml(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Docker Compose file: {composeFilePath}", ex);
        }

        if (composeFile?.Services is null || composeFile.Services.Count == 0)
        {
            warnings.Add($"No services found in Docker Compose file: {composeFilePath}");
            return;
        }

        foreach (var (serviceName, service) in composeFile.Services)
        {
            try
            {
                ImportService(builder, parentResource, serviceName, service, warnings);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to import service '{serviceName}' from Docker Compose file: {ex.Message}");
            }
        }
    }

    private static void ImportService(IDistributedApplicationBuilder builder, DockerComposeFileResource parentResource, string serviceName, Service service, List<string> warnings)
    {
        IResourceBuilder<ContainerResource> containerBuilder;

        // Check if service has a build configuration
        if (service.Build is not null)
        {
            // Use AddDockerfile for services with build configurations
            var contextPath = service.Build.Context ?? ".";
            var dockerfilePath = service.Build.Dockerfile;
            var stage = service.Build.Target;

            containerBuilder = builder.AddDockerfile(serviceName, contextPath, dockerfilePath, stage)
                .WithAnnotation(new ResourceRelationshipAnnotation(parentResource, "parent"));
            
            // Add build args if present
            if (service.Build.Args is not null && service.Build.Args.Count > 0)
            {
                foreach (var (key, value) in service.Build.Args)
                {
                    containerBuilder.WithBuildArg(key, value);
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(service.Image))
        {
            // Use AddContainer for services with pre-built images
            // Parse image using ContainerReferenceParser
            ContainerReference containerRef;
            try
            {
                containerRef = ContainerReferenceParser.Parse(service.Image);
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to parse image reference '{service.Image}' for service '{serviceName}': {ex.Message}");
                return;
            }

            var imageName = containerRef.Registry is null 
                ? containerRef.Image 
                : $"{containerRef.Registry}/{containerRef.Image}";
            var imageTag = containerRef.Tag ?? "latest";

            containerBuilder = builder.AddContainer(serviceName, imageName, imageTag)
                .WithAnnotation(new ResourceRelationshipAnnotation(parentResource, "parent"));
        }
        else
        {
            // Skip services without an image or build configuration
            warnings.Add($"Service '{serviceName}' has neither image nor build configuration. Skipping.");
            return;
        }

        // Import environment variables
        if (service.Environment is not null && service.Environment.Count > 0)
        {
            foreach (var (key, value) in service.Environment)
            {
                containerBuilder.WithEnvironment(key, value);
            }
        }

        // Import ports
        if (service.Ports is not null && service.Ports.Count > 0)
        {
            foreach (var portMapping in service.Ports)
            {
                if (TryParsePortMapping(portMapping, out var hostPort, out var containerPort, out var protocol))
                {
                    // Determine scheme based on protocol
                    var scheme = protocol?.ToLowerInvariant() == "tcp" ? "tcp" : "http";
                    
                    // Create endpoint name from port mapping
                    var endpointName = hostPort.HasValue ? $"port{hostPort.Value}" : $"port{containerPort}";
                    
                    containerBuilder.WithEndpoint(
                        name: endpointName,
                        scheme: scheme,
                        port: hostPort,
                        targetPort: containerPort,
                        isExternal: true);
                }
            }
        }

        // Import volumes
        if (service.Volumes is not null && service.Volumes.Count > 0)
        {
            foreach (var volume in service.Volumes)
            {
                if (!string.IsNullOrWhiteSpace(volume.Source) && !string.IsNullOrWhiteSpace(volume.Target))
                {
                    var isReadOnly = volume.ReadOnly ?? false;
                    if (volume.Type == "bind")
                    {
                        containerBuilder.WithBindMount(volume.Source, volume.Target, isReadOnly);
                    }
                    else
                    {
                        containerBuilder.WithVolume(volume.Source, volume.Target, isReadOnly);
                    }
                }
            }
        }

        // Import command
        if (service.Command is not null && service.Command.Count > 0)
        {
            containerBuilder.WithArgs(service.Command.ToArray());
        }

        // Import entrypoint  
        if (service.Entrypoint is not null && service.Entrypoint.Count > 0)
        {
            // WithEntrypoint expects a single string, so join them with space
            containerBuilder.WithEntrypoint(string.Join(" ", service.Entrypoint));
        }
    }

    private static bool TryParsePortMapping(string portMapping, out int? hostPort, out int? containerPort, out string? protocol)
    {
        hostPort = null;
        containerPort = null;
        protocol = null;

        // Expected formats:
        // "8080:80"
        // "8080:80/tcp"
        // "127.0.0.1:8080:80"
        // "80" (just container port, no host port)

        var parts = portMapping.Split('/');
        var portPart = parts[0];
        if (parts.Length > 1)
        {
            protocol = parts[1];
        }

        var portParts = portPart.Split(':');

        if (portParts.Length == 1)
        {
            // Just container port
            if (int.TryParse(portParts[0], out var port))
            {
                containerPort = port;
                return true;
            }
        }
        else if (portParts.Length == 2)
        {
            // host:container
            if (int.TryParse(portParts[0], out var hPort) && int.TryParse(portParts[1], out var cPort))
            {
                hostPort = hPort;
                containerPort = cPort;
                return true;
            }
        }
        else if (portParts.Length == 3)
        {
            // IP:host:container - skip IP and use host:container
            if (int.TryParse(portParts[1], out var hPort) && int.TryParse(portParts[2], out var cPort))
            {
                hostPort = hPort;
                containerPort = cPort;
                return true;
            }
        }

        return false;
    }
}
