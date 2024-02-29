// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal class ManifestPublisher(ILogger<ManifestPublisher> logger,
                               IOptions<PublishingOptions> options,
                               IHostApplicationLifetime lifetime,
                               DistributedApplicationExecutionContext executionContext) : IDistributedApplicationPublisher
{
    private readonly ILogger<ManifestPublisher> _logger = logger;
    private readonly IOptions<PublishingOptions> _options = options;
    private readonly IHostApplicationLifetime _lifetime = lifetime;
    private readonly DistributedApplicationExecutionContext _executionContext = executionContext;

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
                "The '--output-path [path]' option was not specified even though '--publisher manifest' argument was used."
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
        var manifestPath = _options.Value.OutputPath ?? throw new DistributedApplicationException("The '--output-path [path]' option was not specified even though '--publisher manifest' argument was used.");
        var context = new ManifestPublishingContext(_executionContext, manifestPath, jsonWriter, cancellationToken);

        jsonWriter.WriteStartObject();
        await WriteResources(model, context).ConfigureAwait(false);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteResources(DistributedApplicationModel model, ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("resources");
        foreach (var resource in model.Resources)
        {
            await WriteResource(resource, context).ConfigureAwait(false);
        }
        context.Writer.WriteEndObject();
    }

    internal static async Task WriteResource(IResource resource, ManifestPublishingContext context)
    {
        // First see if the resource has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            if (manifestPublishingCallbackAnnotation.Callback != null)
            {
                await WriteResourceObject(resource, () => manifestPublishingCallbackAnnotation.Callback(context)).ConfigureAwait(false);
            }
        }
        else if (resource is ContainerResource container)
        {
            await WriteResourceObject(container, () => context.WriteContainer(container)).ConfigureAwait(false);
        }
        else if (resource is ProjectResource project)
        {
            await WriteResourceObject(project, () => WriteProject(project, context)).ConfigureAwait(false);
        }
        else if (resource is ExecutableResource executable)
        {
            await WriteResourceObject(executable, () => WriteExecutable(executable, context)).ConfigureAwait(false);
        }
        else
        {
            await WriteResourceObject(resource, () => WriteError(context)).ConfigureAwait(false);
        }

        async Task WriteResourceObject<T>(T resource, Func<Task> action) where T : IResource
        {
            context.Writer.WriteStartObject(resource.Name);
            await action().ConfigureAwait(false);
            context.WriteManifestMetadata(resource);
            context.Writer.WriteEndObject();
        }
    }

    private static Task WriteError(ManifestPublishingContext context)
    {
        context.Writer.WriteString("error", "This resource does not support generation in the manifest.");
        return Task.CompletedTask;
    }

    private static async Task WriteProject(ProjectResource project, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Project metadata not found.");
        }

        var relativePathToProjectFile = context.GetManifestRelativePath(metadata.ProjectPath);

        context.Writer.WriteString("path", relativePathToProjectFile);

        await context.WriteEnvironmentVariables(project).ConfigureAwait(false);
        context.WriteBindings(project);
    }

    private static async Task WriteExecutable(ExecutableResource executable, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "executable.v0");

        // Write the connection string if it exists.
        context.WriteConnectionString(executable);

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

        await context.WriteEnvironmentVariables(executable).ConfigureAwait(false);
        context.WriteBindings(executable);
    }
}
