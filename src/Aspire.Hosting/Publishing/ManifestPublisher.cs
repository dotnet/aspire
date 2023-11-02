// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

public class ManifestPublisher(ILogger<ManifestPublisher> logger, IOptions<PublishingOptions> options, IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly ILogger<ManifestPublisher> _logger = logger;
    private readonly IOptions<PublishingOptions> _options = options;
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    public Utf8JsonWriter? JsonWriter { get; set; }

    public virtual async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        await PublishInternalAsync(model, cancellationToken).ConfigureAwait(false);
        _lifetime.StopApplication();
    }

    protected virtual async Task PublishInternalAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (_options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publish manifest' argument was used."
                );
        }

        using var stream = new FileStream(_options.Value.OutputPath, FileMode.Create);
        using var jsonWriter = JsonWriter ?? new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        await WriteManifestAsync(model, jsonWriter, cancellationToken).ConfigureAwait(false);

        var fullyQualifiedPath = Path.GetFullPath(_options.Value.OutputPath);
        _logger.LogInformation("Published manifest to: {manifestPath}", fullyQualifiedPath);
    }

    protected async Task WriteManifestAsync(DistributedApplicationModel model, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteStartObject();
        WriteResources(model, jsonWriter);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResources(DistributedApplicationModel model, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartObject("resources");
        foreach (var resource in model.Resources)
        {
            WriteResource(resource, jsonWriter);
        }
        jsonWriter.WriteEndObject();
    }

    private void WriteResource(IResource resource, Utf8JsonWriter jsonWriter)
    {
        // First see if the resource has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            if (manifestPublishingCallbackAnnotation.Callback != null)
            {
                WriteResourceObject(resource, () => manifestPublishingCallbackAnnotation.Callback(jsonWriter));
            }
        }
        else if (resource is ContainerResource container)
        {
            WriteResourceObject(container, () => WriteContainer(container, jsonWriter));
        }
        else if (resource is ProjectResource project)
        {
            WriteResourceObject(project, () => WriteProject(project, jsonWriter));
        }
        else if (resource is ExecutableResource executable)
        {
            WriteResourceObject(executable, () => WriteExecutable(executable, jsonWriter));
        }
        else
        {
            WriteResourceObject(resource, () => WriteError(jsonWriter));
        }

        void WriteResourceObject<T>(T resource, Action action) where T: IResource
        {
            jsonWriter.WriteStartObject(resource.Name);
            action();
            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteError(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("error", "This resource does not support generation in the manifest.");
    }

    private static void WriteServiceDiscoveryEnvironmentVariables(IResource resource, Utf8JsonWriter jsonWriter)
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

                    jsonWriter.WriteString($"services__{serviceReferenceAnnotation.Resource.Name}__{i++}", $"{{{serviceReferenceAnnotation.Resource.Name}.bindings.{binding.Name}.url}}");
                }

            }
        }
    }

    private static void WriteEnvironmentVariables(IResource resource, Utf8JsonWriter jsonWriter)
    {
        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("manifest", config);

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
        {
            jsonWriter.WriteStartObject("env");
            foreach (var callback in callbacks)
            {
                callback.Callback(context);
            }

            foreach (var (key, value) in config)
            {
                jsonWriter.WriteString(key, value);
            }

            WriteServiceDiscoveryEnvironmentVariables(resource, jsonWriter);

            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteBindings(IResource resource, Utf8JsonWriter jsonWriter)
    {
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            jsonWriter.WriteStartObject("bindings");
            foreach (var serviceBinding in serviceBindings)
            {
                jsonWriter.WriteStartObject(serviceBinding.Name);
                jsonWriter.WriteString("scheme", serviceBinding.UriScheme);
                jsonWriter.WriteString("protocol", serviceBinding.Protocol.ToString().ToLowerInvariant());
                jsonWriter.WriteString("transport", serviceBinding.Transport);

                if (serviceBinding.IsExternal)
                {
                    jsonWriter.WriteBoolean("external", serviceBinding.IsExternal);
                }

                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteContainer(ContainerResource container, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "container.v0");

        if (!container.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        jsonWriter.WriteString("image", image);

        WriteEnvironmentVariables(container, jsonWriter);
        WriteBindings(container, jsonWriter);
    }

    private void WriteProject(ProjectResource project, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IServiceMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Service metadata not found.");
        }

        var manifestPath = _options.Value.OutputPath ?? throw new DistributedApplicationException("Output path not specified");
        var fullyQualifiedManifestPath = Path.GetFullPath(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullyQualifiedManifestPath) ?? throw new DistributedApplicationException("Could not get directory name of output path");
        var relativePathToProjectFile = Path.GetRelativePath(manifestDirectory, metadata.ProjectPath);
        jsonWriter.WriteString("path", relativePathToProjectFile);

        WriteEnvironmentVariables(project, jsonWriter);
        WriteBindings(project, jsonWriter);
    }

    private static void WriteExecutable(ExecutableResource executable, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "executable.v0");

        WriteEnvironmentVariables(executable, jsonWriter);
        WriteBindings(executable, jsonWriter);
    }
}
