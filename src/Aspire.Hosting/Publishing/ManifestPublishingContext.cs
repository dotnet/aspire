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
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            Writer.WriteStartObject("bindings");
            foreach (var serviceBinding in serviceBindings)
            {
                Writer.WriteStartObject(serviceBinding.Name);
                Writer.WriteString("scheme", serviceBinding.UriScheme);
                Writer.WriteString("protocol", serviceBinding.Protocol.ToString().ToLowerInvariant());
                Writer.WriteString("transport", serviceBinding.Transport);

                if (emitContainerPort && serviceBinding.ContainerPort is { } containerPort)
                {
                    Writer.WriteNumber("containerPort", containerPort);
                }

                if (serviceBinding.IsExternal)
                {
                    Writer.WriteBoolean("external", serviceBinding.IsExternal);
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
        var serviceReferenceAnnotations = resource.Annotations.OfType<ServiceReferenceAnnotation>();

        if (serviceReferenceAnnotations.Any())
        {
            foreach (var serviceReferenceAnnotation in serviceReferenceAnnotations)
            {
                var bindingNames = serviceReferenceAnnotation.UseAllBindings
                    ? serviceReferenceAnnotation.Resource.Annotations.OfType<ServiceBindingAnnotation>().Select(sb => sb.Name)
                    : serviceReferenceAnnotation.BindingNames;

                var serviceBindingAnnotationsGroupedByScheme = serviceReferenceAnnotation.Resource.Annotations
                    .OfType<ServiceBindingAnnotation>()
                    .Where(sba => bindingNames.Contains(sba.Name))
                    .GroupBy(sba => sba.UriScheme);

                var i = 0;
                foreach (var serviceBindingAnnotationGroupedByScheme in serviceBindingAnnotationsGroupedByScheme)
                {
                    // HACK: For November we are only going to support a single service binding annotation
                    //       per URI scheme per service reference.
                    var binding = serviceBindingAnnotationGroupedByScheme.Single();

                    Writer.WriteString($"services__{serviceReferenceAnnotation.Resource.Name}__{i++}", $"{{{serviceReferenceAnnotation.Resource.Name}.bindings.{binding.Name}.url}}");
                }

            }
        }
    }

    public void WritePortBindingEnvironmentVariables(IResource resource)
    {
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            foreach (var serviceBinding in serviceBindings)
            {
                if (serviceBinding.EnvironmentVariable is null)
                {
                    continue;
                }

                Writer.WriteString(serviceBinding.EnvironmentVariable, $"{{{resource.Name}.bindings.{serviceBinding.Name}.port}}");
            }
        }
    }
}
