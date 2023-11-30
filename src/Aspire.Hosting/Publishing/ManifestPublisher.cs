// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

public class ManifestPublisher(ILogger<ManifestPublisher> logger,
                               IOptions<PublishingOptions> options,
                               IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
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
        var manifestPath = _options.Value.OutputPath ?? throw new DistributedApplicationException("The '--output-path [path]' option was not specified even though '--publish manifest' argument was used.");
        var context = new ManifestPublishingContext(manifestPath, jsonWriter);

        jsonWriter.WriteStartObject();
        WriteResources(model, context);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteResources(DistributedApplicationModel model, ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("resources");
        foreach (var resource in model.Resources)
        {
            WriteResource(resource, context);
        }
        context.Writer.WriteEndObject();
    }

    private void WriteResource(IResource resource, ManifestPublishingContext context)
    {
        // First see if the resource has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            if (manifestPublishingCallbackAnnotation.Callback != null)
            {
                WriteResourceObject(resource, () => manifestPublishingCallbackAnnotation.Callback(context));
            }
        }
        else if (resource is ContainerResource container)
        {
            WriteResourceObject(container, () => WriteContainer(container, context));
        }
        else if (resource is ProjectResource project)
        {
            WriteResourceObject(project, () => WriteProject(project, context));
        }
        else if (resource is ExecutableResource executable)
        {
            WriteResourceObject(executable, () => WriteExecutable(executable, context));
        }
        else
        {
            WriteResourceObject(resource, () => WriteError(context));
        }

        void WriteResourceObject<T>(T resource, Action action) where T : IResource
        {
            context.Writer.WriteStartObject(resource.Name);
            action();
            context.Writer.WriteEndObject();
        }
    }

    private static void WriteError(ManifestPublishingContext context)
    {
        context.Writer.WriteString("error", "This resource does not support generation in the manifest.");
    }

    private static void WriteServiceDiscoveryEnvironmentVariables(IResource resource, ManifestPublishingContext context)
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

                    context.Writer.WriteString($"services__{serviceReferenceAnnotation.Resource.Name}__{i++}", $"{{{serviceReferenceAnnotation.Resource.Name}.bindings.{binding.Name}.url}}");
                }

            }
        }
    }
    internal static void WriteDependencies(IResource resource, ManifestPublishingContext context)
    {
        var dependencies = resource.GetDependencies();

        if (dependencies.Any())
        {
            context.Writer.WriteStartObject("dependencies");
            foreach (var d in dependencies)
            {
                context.Writer.WriteStartObject(d.Name);
                // TODO: We'll want to write dependency edge information here eventually.
                context.Writer.WriteEndObject();
            }
            context.Writer.WriteEndObject();
        }
    }

    internal static void WriteEnvironmentVariables(IResource resource, ManifestPublishingContext context)
    {
        var config = new Dictionary<string, string>();
        var envContext = new EnvironmentCallbackContext("manifest", config);

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
        {
            context.Writer.WriteStartObject("env");
            foreach (var callback in callbacks)
            {
                callback.Callback(envContext);
            }

            foreach (var (key, value) in config)
            {
                context.Writer.WriteString(key, value);
            }

            WriteServiceDiscoveryEnvironmentVariables(resource, context);

            WritePortBindingEnvironmentVariables(resource, context);

            context.Writer.WriteEndObject();
        }
    }

    private static void WritePortBindingEnvironmentVariables(IResource resource, ManifestPublishingContext context)
    {
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            foreach (var serviceBinding in serviceBindings)
            {
                if (serviceBinding.EnvironmentVariable is null)
                {
                    continue;
                }

                context.Writer.WriteString(serviceBinding.EnvironmentVariable, $"{{{resource.Name}.bindings.{serviceBinding.Name}.port}}");
            }
        }
    }

    internal static void WriteBindings(IResource resource, ManifestPublishingContext context, bool emitContainerPort = false)
    {
        if (resource.TryGetServiceBindings(out var serviceBindings))
        {
            context.Writer.WriteStartObject("bindings");
            foreach (var serviceBinding in serviceBindings)
            {
                context.Writer.WriteStartObject(serviceBinding.Name);
                context.Writer.WriteString("scheme", serviceBinding.UriScheme);
                context.Writer.WriteString("protocol", serviceBinding.Protocol.ToString().ToLowerInvariant());
                context.Writer.WriteString("transport", serviceBinding.Transport);

                if (emitContainerPort && serviceBinding.ContainerPort is { } containerPort)
                {
                    context.Writer.WriteNumber("containerPort", containerPort);
                }

                if (serviceBinding.IsExternal)
                {
                    context.Writer.WriteBoolean("external", serviceBinding.IsExternal);
                }

                context.Writer.WriteEndObject();
            }
            context.Writer.WriteEndObject();
        }
    }

    private static void WriteContainer(ContainerResource container, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "container.v0");

        if (!container.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        context.Writer.WriteString("image", image);

        WriteDependencies(container, context);
        WriteEnvironmentVariables(container, context);
        WriteBindings(container, context, emitContainerPort: true);
    }

    private static void WriteProject(ProjectResource project, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IServiceMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Service metadata not found.");
        }

        var relativePathToProjectFile = context.GetManifestRelativePath(metadata.ProjectPath);

        context.Writer.WriteString("path", relativePathToProjectFile);

        WriteDependencies(project, context);
        WriteEnvironmentVariables(project, context);
        WriteBindings(project, context);
    }

    private void WriteExecutable(ExecutableResource executable, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "executable.v0");

        var relativePathToProjectFile = context.GetManifestRelativePath(executable.WorkingDirectory);

        context.Writer.WriteString("workingDirectory", relativePathToProjectFile);

        context.Writer.WriteString("command", executable.Command);
        context.Writer.WriteStartArray("args");

        foreach (var arg in executable.Args ?? [])
        {
            context.Writer.WriteStringValue(arg);
        }
        context.Writer.WriteEndArray();

        WriteDependencies(executable, context);
        WriteEnvironmentVariables(executable, context);
        WriteBindings(executable, context);
    }
}
