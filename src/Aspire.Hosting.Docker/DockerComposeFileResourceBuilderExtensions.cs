// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;
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

        // Resolve the compose file path to a full physical path relative to the app host directory
        var fullComposeFilePath = Path.GetFullPath(composeFilePath, builder.AppHostDirectory);

        var resource = new DockerComposeFileResource(name, fullComposeFilePath);
        
        // Parse and import the compose file synchronously to add resources to the model
        // Capture any exceptions to report during initialization
        Exception? parseException = null;
        List<string> warnings = [];
        
        try
        {
            ParseAndImportComposeFile(builder, resource, fullComposeFilePath, warnings);
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
                return;
            }

            foreach (var warning in warnings)
            {
                e.Logger.LogWarning("{Warning}", warning);
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
        
        Dictionary<string, ParsedService> services;
        try
        {
            services = DockerComposeParser.ParseComposeFile(yamlContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Docker Compose file: {composeFilePath}", ex);
        }

        if (services.Count == 0)
        {
            warnings.Add($"No services found in Docker Compose file: {composeFilePath}");
            return;
        }

        // First pass: Create all container resources
        foreach (var (serviceName, service) in services)
        {
            try
            {
                var containerBuilder = ImportService(builder, parentResource, serviceName, service, composeFilePath, warnings);
                if (containerBuilder is not null)
                {
                    parentResource.ServiceBuilders[serviceName] = containerBuilder;
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to import service '{serviceName}' from Docker Compose file: {ex.Message}");
            }
        }

        // Second pass: Set up dependencies (depends_on)
        foreach (var (serviceName, service) in services)
        {
            if (service.DependsOn.Count == 0)
            {
                continue;
            }

            if (!parentResource.ServiceBuilders.TryGetValue(serviceName, out var containerBuilder))
            {
                continue; // Service was skipped
            }

            foreach (var (dependencyName, dependency) in service.DependsOn)
            {
                if (!parentResource.ServiceBuilders.TryGetValue(dependencyName, out var dependencyBuilder))
                {
                    warnings.Add($"Service '{serviceName}' depends on '{dependencyName}', but '{dependencyName}' was not imported.");
                    continue;
                }

                try
                {
                    // Map Docker Compose condition to Aspire WaitFor methods
                    var condition = dependency.Condition?.ToLowerInvariant();
                    switch (condition)
                    {
                        case "service_started":
                            containerBuilder.WaitForStart(dependencyBuilder);
                            break;
                        case "service_healthy":
                            containerBuilder.WaitFor(dependencyBuilder);
                            break;
                        case "service_completed_successfully":
                            containerBuilder.WaitForCompletion(dependencyBuilder);
                            break;
                        case null:
                        case "":
                            // Default behavior: wait for service to start
                            containerBuilder.WaitForStart(dependencyBuilder);
                            break;
                        default:
                            warnings.Add($"Unknown depends_on condition '{dependency.Condition}' for service '{serviceName}' -> '{dependencyName}'. Using default (service_started).");
                            containerBuilder.WaitForStart(dependencyBuilder);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Failed to set up dependency for service '{serviceName}' -> '{dependencyName}': {ex.Message}");
                }
            }
        }
    }

    private static IResourceBuilder<ContainerResource>? ImportService(IDistributedApplicationBuilder builder, DockerComposeFileResource parentResource, string serviceName, ParsedService service, string composeFilePath, List<string> warnings)
    {
        IResourceBuilder<ContainerResource> containerBuilder;

        // Check if service has a build configuration
        if (service.Build is not null)
        {
            // Use AddDockerfile for services with build configurations
            // Resolve context path relative to the compose file's directory
            var contextPath = service.Build.Context ?? ".";
            var composeFileDirectory = Path.GetDirectoryName(composeFilePath)!;
            var resolvedContextPath = Path.GetFullPath(contextPath, composeFileDirectory);
            
            var dockerfilePath = service.Build.Dockerfile;
            var stage = service.Build.Target;

            containerBuilder = builder.AddDockerfile(serviceName, resolvedContextPath, dockerfilePath, stage)
                .WithAnnotation(new ResourceRelationshipAnnotation(parentResource, "parent"));
            
            // Add build args if present
            if (service.Build.Args.Count > 0)
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
                return null;
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
            return null;
        }

        // Import environment variables
        if (service.Environment.Count > 0)
        {
            foreach (var (key, value) in service.Environment)
            {
                containerBuilder.WithEnvironment(key, value);
            }
        }

        // Import ports
        if (service.Ports.Count > 0)
        {
            foreach (var port in service.Ports)
            {
                if (port.Target.HasValue)
                {
                    // Determine scheme based on protocol
                    // Short syntax with explicit /tcp → use tcp scheme (for raw TCP connections)
                    // Long syntax with protocol:tcp → convert to http (common web scenario)
                    // No protocol specified → default to http
                    // UDP → use udp scheme
                    var scheme = port.Protocol?.ToLowerInvariant() switch
                    {
                        "udp" => "udp",
                        "tcp" when port.IsShortSyntax => "tcp", // Short syntax /tcp means raw TCP
                        "tcp" => "http", // Long syntax tcp means HTTP over TCP
                        null => "http",
                        _ => "http"
                    };
                    
                    // Use the port name from long syntax if available, otherwise generate one
                    var endpointName = !string.IsNullOrWhiteSpace(port.Name) 
                        ? port.Name 
                        : (port.Published.HasValue ? $"port{port.Published.Value}" : $"port{port.Target.Value}");
                    
                    containerBuilder.WithEndpoint(
                        name: endpointName,
                        scheme: scheme,
                        port: port.Published,
                        targetPort: port.Target.Value,
                        isExternal: true,
                        isProxied: false);
                }
            }
        }

        // Import volumes
        if (service.Volumes.Count > 0)
        {
            foreach (var volume in service.Volumes)
            {
                if (!string.IsNullOrWhiteSpace(volume.Target))
                {
                    if (string.IsNullOrWhiteSpace(volume.Source))
                    {
                        // Anonymous volume - just target path
                        containerBuilder.WithVolume(volume.Target);
                    }
                    else
                    {
                        var isReadOnly = volume.ReadOnly;
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
        }

        // Import command
        if (service.Command.Count > 0)
        {
            containerBuilder.WithArgs(service.Command.ToArray());
        }

        // Import entrypoint  
        if (service.Entrypoint.Count > 0)
        {
            // WithEntrypoint expects a single string, so join them with space
            containerBuilder.WithEntrypoint(string.Join(" ", service.Entrypoint));
        }

        return containerBuilder;
    }

    /// <summary>
    /// Gets a container resource builder for a specific service defined in the Docker Compose file.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{DockerComposeFileResource}"/>.</param>
    /// <param name="serviceName">The name of the service as defined in the docker-compose.yml file.</param>
    /// <returns>The <see cref="IResourceBuilder{ContainerResource}"/> for the specified service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not found in the compose file.</exception>
    /// <example>
    /// <code>
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var compose = builder.AddDockerComposeFile("mycompose", "./docker-compose.yml");
    /// 
    /// // Get a reference to a specific service to configure it further
    /// var webService = compose.GetComposeService("web");
    /// webService.WithEnvironment("ADDITIONAL_VAR", "value");
    ///
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ContainerResource> GetComposeService(
        this IResourceBuilder<DockerComposeFileResource> builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        if (!builder.Resource.ServiceBuilders.TryGetValue(serviceName, out var serviceBuilder))
        {
            throw new InvalidOperationException($"Service '{serviceName}' not found in Docker Compose file '{builder.Resource.ComposeFilePath}'. Available services: {string.Join(", ", builder.Resource.ServiceBuilders.Keys)}");
        }

        return serviceBuilder;
    }
}
