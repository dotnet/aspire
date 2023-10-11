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
        WriteComponents(model, jsonWriter);
        jsonWriter.WriteEndObject();

        await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void WriteComponents(DistributedApplicationModel model, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartObject("components");
        foreach (var component in model.Components)
        {
            WriteComponent(component, jsonWriter);
        }
        jsonWriter.WriteEndObject();
    }

    private void WriteComponent(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartObject(component.Name);

        // First see if the component has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (component.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            manifestPublishingCallbackAnnotation.Callback(jsonWriter);
        }
        else if (component is ContainerComponent containerComponent)
        {
            WriteContainer(containerComponent, jsonWriter);
        }
        else if (component is ProjectComponent projectComponent)
        {
            WriteProject(projectComponent, jsonWriter);
        }
        else if (component is ExecutableComponent executableComponent)
        {
            WriteExecutable(executableComponent, jsonWriter);
        }
        else
        {
            WriteError(jsonWriter);
        }

        jsonWriter.WriteEndObject();
    }

    private static void WriteError(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("error", "This component does not support generation in the manifest.");
    }

    private static void WriteEnvironmentVariables(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter)
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
    }

    private static void WriteBindings(IDistributedApplicationComponent component, Utf8JsonWriter jsonWriter)
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

                if (serviceBinding.IsExternal)
                {
                    jsonWriter.WriteBoolean("external", serviceBinding.IsExternal);
                }

                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndObject();
        }
    }

    private static void WriteContainer(ContainerComponent containerComponent, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "container.v1");

        if (!containerComponent.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        jsonWriter.WriteString("image", image);

        WriteEnvironmentVariables(containerComponent, jsonWriter);
        WriteBindings(containerComponent, jsonWriter);
    }

    private void WriteProject(ProjectComponent projectComponent, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "project.v1");

        if (!projectComponent.TryGetLastAnnotation<IServiceMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Service metadata not found.");
        }

        var manifestPath = _options.Value.OutputPath ?? throw new DistributedApplicationException("Output path not specified");
        var relativePathToProjectFile = Path.GetRelativePath(Path.GetDirectoryName(manifestPath)!, metadata.ProjectPath);
        jsonWriter.WriteString("path", relativePathToProjectFile);

        WriteEnvironmentVariables(projectComponent, jsonWriter);
        WriteBindings(projectComponent, jsonWriter);
    }

    private static void WriteExecutable(ExecutableComponent executableComponent, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "executable.v1");

        WriteEnvironmentVariables(executableComponent, jsonWriter);
        WriteBindings(executableComponent, jsonWriter);
    }
}
