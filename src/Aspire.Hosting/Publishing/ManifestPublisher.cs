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
        _logger.LogInformation("Published manifest to: {ManifestPath}", fullyQualifiedPath);
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

    private static void WriteResources(DistributedApplicationModel model, ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("resources");
        foreach (var resource in model.Resources)
        {
            WriteResource(resource, context);
        }
        context.Writer.WriteEndObject();
    }

    private static void WriteResource(IResource resource, ManifestPublishingContext context)
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
            WriteResourceObject(container, () => context.WriteContainer(container));
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

    private static void WriteProject(ProjectResource project, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IServiceMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Service metadata not found.");
        }

        var relativePathToProjectFile = context.GetManifestRelativePath(metadata.ProjectPath);

        context.Writer.WriteString("path", relativePathToProjectFile);

        context.WriteEnvironmentVariables(project);
        context.WriteBindings(project);
    }

    private static void WriteExecutable(ExecutableResource executable, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "executable.v0");

        var relativePathToProjectFile = context.GetManifestRelativePath(executable.WorkingDirectory);

        context.Writer.WriteString("workingDirectory", relativePathToProjectFile);

        context.Writer.WriteString("command", executable.Command);

        var args = new List<string>(executable.Args ?? []);
        if (executable.TryGetAnnotationsOfType<ExecutableArgsCallbackAnnotation>(out var argsCallback))
        {
            foreach (var callback in argsCallback)
            {
                callback.Callback(args);
            }
        }

        if (args.Count > 0)
        {
            context.Writer.WriteStartArray("args");

            foreach (var arg in args)
            {
                context.Writer.WriteStringValue(arg);
            }
            context.Writer.WriteEndArray();
        }

        context.WriteEnvironmentVariables(executable);
        context.WriteBindings(executable);
    }
}
