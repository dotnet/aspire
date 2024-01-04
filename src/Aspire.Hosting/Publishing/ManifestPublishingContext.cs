// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Publishing;

public sealed class ManifestPublishingContext(string manifestPath, Utf8JsonWriter writer)
{
    public string ManifestPath { get; } = manifestPath;

    public Utf8JsonWriter Writer { get; } = writer;

    [return: NotNullIfNotNull(nameof(path))]
    public string? GetManifestRelativePath(string? path)
    {
        if (path is null)
        {
            return null;
        }

        var fullyQualifiedManifestPath = Path.GetFullPath(ManifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullyQualifiedManifestPath) ?? throw new DistributedApplicationException("Could not get directory name of output path");
        var relativePath = Path.GetRelativePath(manifestDirectory, path);

        return relativePath.Replace('\\', '/');
    }

    public void WriteContainer(ContainerResource container)
    {
        Writer.WriteString("type", "container.v0");

        if (!container.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        Writer.WriteString("image", image);

        if (container.Entrypoint is not null)
        {
            Writer.WriteString("entrypoint", container.Entrypoint);
        }

        if (container.TryGetAnnotationsOfType<ExecutableArgsCallbackAnnotation>(out var argsCallback))
        {
            var args = new List<string>();
            foreach (var callback in argsCallback)
            {
                callback.Callback(args);
            }

            if (args.Count > 0)
            {
                Writer.WriteStartArray("args");

                foreach (var arg in args)
                {
                    Writer.WriteStringValue(arg);
                }
                Writer.WriteEndArray();
            }
        }

        WriteEnvironmentVariables(container);
        WriteBindings(container, emitContainerPort: true);
    }

    public void WriteBindings(IResource resource, bool emitContainerPort = false)
    {
        if (resource.TryGetEndpoints(out var endpoints))
        {
            Writer.WriteStartObject("bindings");
            foreach (var endpoint in endpoints)
            {
                Writer.WriteStartObject(endpoint.Name);
                Writer.WriteString("scheme", endpoint.UriScheme);
                Writer.WriteString("protocol", endpoint.Protocol.ToString().ToLowerInvariant());
                Writer.WriteString("transport", endpoint.Transport);

                if (emitContainerPort && endpoint.ContainerPort is { } containerPort)
                {
                    Writer.WriteNumber("containerPort", containerPort);
                }

                if (endpoint.IsExternal)
                {
                    Writer.WriteBoolean("external", endpoint.IsExternal);
                }

                Writer.WriteEndObject();
            }
            Writer.WriteEndObject();
        }
    }

    public void WriteEnvironmentVariables(IResource resource)
    {
        var config = new Dictionary<string, string>();
        var envContext = new EnvironmentCallbackContext("manifest", config);

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
        {
            Writer.WriteStartObject("env");
            foreach (var callback in callbacks)
            {
                callback.Callback(envContext);
            }

            foreach (var (key, value) in config)
            {
                Writer.WriteString(key, value);
            }

            WriteServiceDiscoveryEnvironmentVariables(resource);

            WritePortBindingEnvironmentVariables(resource);

            Writer.WriteEndObject();
        }
    }

    public void WriteServiceDiscoveryEnvironmentVariables(IResource resource)
    {
        var endpointReferenceAnnotations = resource.Annotations.OfType<EndpointReferenceAnnotation>();

        if (endpointReferenceAnnotations.Any())
        {
            foreach (var endpointReferenceAnnotation in endpointReferenceAnnotations)
            {
                var endpointNames = endpointReferenceAnnotation.UseAllEndpoints
                    ? endpointReferenceAnnotation.Resource.Annotations.OfType<EndpointAnnotation>().Select(sb => sb.Name)
                    : endpointReferenceAnnotation.EndpointNames;

                var endpointAnnotationsGroupedByScheme = endpointReferenceAnnotation.Resource.Annotations
                    .OfType<EndpointAnnotation>()
                    .Where(sba => endpointNames.Contains(sba.Name))
                    .GroupBy(sba => sba.UriScheme);

                var i = 0;
                foreach (var endpointAnnotationGroupedByScheme in endpointAnnotationsGroupedByScheme)
                {
                    // HACK: For November we are only going to support a single endpoint annotation
                    //       per URI scheme per service reference.
                    var binding = endpointAnnotationGroupedByScheme.Single();

                    Writer.WriteString($"services__{endpointReferenceAnnotation.Resource.Name}__{i++}", $"{{{endpointReferenceAnnotation.Resource.Name}.bindings.{binding.Name}.url}}");
                }

            }
        }
    }

    public void WritePortBindingEnvironmentVariables(IResource resource)
    {
        if (resource.TryGetEndpoints(out var endpoints))
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.EnvironmentVariable is null)
                {
                    continue;
                }

                Writer.WriteString(endpoint.EnvironmentVariable, $"{{{resource.Name}.bindings.{endpoint.Name}.port}}");
            }
        }
    }
}
