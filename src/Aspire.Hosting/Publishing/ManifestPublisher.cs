// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Publishing;

internal sealed class ManifestPublisher(IOptions<PublishingOptions> options, IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly IOptions<PublishingOptions> _options = options;
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    public string Name => "manifest";

    public async Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        await WriteManifestAsync(model, cancellationToken).ConfigureAwait(false);
        _lifetime.StopApplication();
    }

    private async Task WriteManifestAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        if (_options.Value.OutputPath == null)
        {
            throw new DistributedApplicationException(
                "The '--output-path [path]' option was not specified even though '--publish manifest' argument was used."
                );
        }

        using var stream = new FileStream(_options.Value.OutputPath, FileMode.Create);
        using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        jsonWriter.WriteStartObject();
        await WriteComponentsAsync(model, jsonWriter, cancellationToken).ConfigureAwait(false);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteComponentsAsync(DistributedApplicationModel model, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteStartObject("components");
        foreach (var component in model.Components)
        {
            await WriteComponentAsync(component, jsonWriter, cancellationToken).ConfigureAwait(false);
        }
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteComponentAsync(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        if (!component.TryGetName(out var componentName))
        {
            throw new DistributedApplicationException("Component did not have name!");
        }

        jsonWriter.WriteStartObject(componentName);

        // First see if the component has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (component.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            await manifestPublishingCallbackAnnotation.Callback(jsonWriter, cancellationToken).ConfigureAwait(false);
        }
        else if (component is ContainerComponent containerComponent)
        {
            await WriteContainerAsync(containerComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        }
        else if (component is ProjectComponent projectComponent)
        {
            await WriteProjectAsync(projectComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        }
        else if (component is ExecutableComponent executableComponent)
        {
            await WriteExecutableAsync(executableComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WriteErrorAsync(jsonWriter, cancellationToken).ConfigureAwait(false);
        }

        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("error", "This component does not support generation in the manifest.");
        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteEnvironmentVariablesAsync(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("manifest", config);

        if (component.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
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
            jsonWriter.WriteEndObject();
        }

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteBindingsAsync(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        if (component.TryGetServiceBindings(out var serviceBindings))
        {
            jsonWriter.WriteStartObject("bindings");
            foreach (var serviceBinding in serviceBindings)
            {
                jsonWriter.WriteStartObject(serviceBinding.Name);
                jsonWriter.WriteString("scheme", serviceBinding.UriScheme);
                jsonWriter.WriteString("protocol", serviceBinding.Protocol.ToString().ToLowerInvariant());
                jsonWriter.WriteString("transport", serviceBinding.Transport);
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteContainerAsync(ContainerComponent containerComponent, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "container.v1");

        if (!containerComponent.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        jsonWriter.WriteString("image", image);

        await WriteEnvironmentVariablesAsync(containerComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        await WriteBindingsAsync(containerComponent, jsonWriter, cancellationToken).ConfigureAwait(false);

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteProjectAsync(ProjectComponent projectComponent, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "project.v1");

        if (!projectComponent.TryGetLastAnnotation<IServiceMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Service metadata not found.");
        }

        var manifestPath = _options.Value.OutputPath ?? throw new DistributedApplicationException("Output path not specified");
        var relativePathToProjectFile = Path.GetRelativePath(manifestPath, metadata.ProjectPath);
        jsonWriter.WriteString("path", relativePathToProjectFile);

        await WriteEnvironmentVariablesAsync(projectComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        await WriteBindingsAsync(projectComponent, jsonWriter, cancellationToken).ConfigureAwait(false);

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteExecutableAsync(ExecutableComponent executableComponent, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteString("type", "executable.v1");

        await WriteEnvironmentVariablesAsync(executableComponent, jsonWriter, cancellationToken).ConfigureAwait(false);
        await WriteBindingsAsync(executableComponent, jsonWriter, cancellationToken).ConfigureAwait(false);

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
