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

    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
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
        await WriteResourcesAsync(model, context).ConfigureAwait(false);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteResourcesAsync(DistributedApplicationModel model, ManifestPublishingContext context)
    {
        context.Writer.WriteStartObject("resources");
        foreach (var resource in model.Resources)
        {
            await WriteResourceAsync(resource, context).ConfigureAwait(false);
        }
        context.Writer.WriteEndObject();
    }

    internal static async Task WriteResourceAsync(IResource resource, ManifestPublishingContext context)
    {
        // First see if the resource has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            if (manifestPublishingCallbackAnnotation.Callback != null)
            {
                await WriteResourceObjectAsync(resource, () => manifestPublishingCallbackAnnotation.Callback(context)).ConfigureAwait(false);
            }
        }
        else if (resource is ContainerResource container)
        {
            await WriteResourceObjectAsync(container, () => context.WriteContainerAsync(container)).ConfigureAwait(false);
        }
        else if (resource is ProjectResource project)
        {
            await WriteResourceObjectAsync(project, () => WriteProjectAsync(project, context)).ConfigureAwait(false);
        }
        else if (resource is ExecutableResource executable)
        {
            await WriteResourceObjectAsync(executable, () => WriteExecutableAsync(executable, context)).ConfigureAwait(false);
        }
        else
        {
            await WriteResourceObjectAsync(resource, () => WriteErrorAsync(context)).ConfigureAwait(false);
        }

        async Task WriteResourceObjectAsync<T>(T resource, Func<Task> action) where T : IResource
        {
            context.Writer.WriteStartObject(resource.Name);
            await action().ConfigureAwait(false);
            context.WriteManifestMetadata(resource);
            context.Writer.WriteEndObject();
        }
    }

    private static Task WriteErrorAsync(ManifestPublishingContext context)
    {
        context.Writer.WriteString("error", "This resource does not support generation in the manifest.");
        return Task.CompletedTask;
    }

    private static async Task WriteProjectAsync(ProjectResource project, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Project metadata not found.");
        }

        var relativePathToProjectFile = context.GetManifestRelativePath(metadata.ProjectPath);

        context.Writer.WriteString("path", relativePathToProjectFile);

        await context.WriteEnvironmentVariablesAsync(project).ConfigureAwait(false);
        context.WriteBindings(project);
    }

    private static async Task WriteExecutableAsync(ExecutableResource executable, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "executable.v0");

        // Write the connection string if it exists.
        context.WriteConnectionString(executable);

        var relativePathToProjectFile = context.GetManifestRelativePath(executable.WorkingDirectory);

        context.Writer.WriteString("workingDirectory", relativePathToProjectFile);

        context.Writer.WriteString("command", executable.Command);

        await context.WriteCommandLineArgumentsAsync(executable).ConfigureAwait(false);

        await context.WriteEnvironmentVariablesAsync(executable).ConfigureAwait(false);
        context.WriteBindings(executable);
    }
}
