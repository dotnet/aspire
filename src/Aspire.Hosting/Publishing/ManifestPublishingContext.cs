// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Contextual information used for manifest publishing during this execution of the AppHost.
/// </summary>
/// <param name="executionContext">Global contextual information for this invocation of the AppHost.</param>
/// <param name="manifestPath">Manifest path passed in for this invocation of the AppHost.</param>
/// <param name="writer">JSON writer used to writing the manifest.</param>
/// <param name="cancellationToken">Cancellation token for this operation.</param>
public sealed class ManifestPublishingContext(DistributedApplicationExecutionContext executionContext, string manifestPath, Utf8JsonWriter writer, CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets execution context for this invocation of the AppHost.
    /// </summary>
    public DistributedApplicationExecutionContext ExecutionContext { get; } = executionContext;

    /// <summary>
    /// Gets manifest path specified for this invocation of the AppHost.
    /// </summary>
    public string ManifestPath { get; } = manifestPath;

    /// <summary>
    /// Gets JSON writer for writing manifest entries.
    /// </summary>
    public Utf8JsonWriter Writer { get; } = writer;

    /// <summary>
    /// Gets cancellation token for this operation.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// Generates a relative path based on the location of the manifest path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="DistributedApplicationException"></exception>
    [return: NotNullIfNotNull(nameof(path))]
    public string? GetManifestRelativePath(string? path)
    {
        if (path is null)
        {
            return null;
        }

        var fullyQualifiedManifestPath = Path.GetFullPath(ManifestPath);
        var manifestDirectory = Path.GetDirectoryName(fullyQualifiedManifestPath) ?? throw new DistributedApplicationException("Could not get directory name of output path");

        var normalizedPath = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var relativePath = Path.GetRelativePath(manifestDirectory, normalizedPath);

        return relativePath.Replace('\\', '/');
    }

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="container"></param>
    /// <exception cref="DistributedApplicationException"></exception>
    public async Task WriteContainerAsync(ContainerResource container)
    {
        Writer.WriteString("type", "container.v0");

        // Attempt to write the connection string for the container (if this resource has one).
        WriteConnectionString(container);

        if (!container.TryGetContainerImageName(out var image))
        {
            throw new DistributedApplicationException("Could not get container image name.");
        }

        Writer.WriteString("image", image);

        if (container.Entrypoint is not null)
        {
            Writer.WriteString("entrypoint", container.Entrypoint);
        }

        // Write args if they are present
        await WriteCommandLineArgumentsAsync(container).ConfigureAwait(false);

        // Write volume & bind mount details
        WriteContainerMounts(container);

        await WriteEnvironmentVariablesAsync(container).ConfigureAwait(false);
        WriteBindings(container, emitContainerPort: true);
        WriteInputs(container);
    }

    /// <summary>
    /// Writes the "connectionString" field for the underlying resource.
    /// </summary>
    /// <param name="resource"></param>
    public void WriteConnectionString(IResource resource)
    {
        if (resource is IResourceWithConnectionString resourceWithConnectionString &&
            resourceWithConnectionString.ConnectionStringExpression is { } connectionString)
        {
            Writer.WriteString("connectionString", connectionString.ValueExpression);
        }
    }

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="emitContainerPort"></param>
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

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="resource"></param>
    public async Task WriteEnvironmentVariablesAsync(IResource resource)
    {
        var config = new Dictionary<string, object>();

        var envContext = new EnvironmentCallbackContext(ExecutionContext, config, CancellationToken);

        if (resource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var callbacks))
        {
            Writer.WriteStartObject("env");
            foreach (var callback in callbacks)
            {
                await callback.Callback(envContext).ConfigureAwait(false);
            }

            foreach (var (key, value) in config)
            {
                var valueString = value switch
                {
                    string stringValue => stringValue,
                    IManifestExpressionProvider manifestExpression => manifestExpression.ValueExpression,
                    _ => throw new DistributedApplicationException($"The value of the environment variable '{key}' is not supported.")
                };

                Writer.WriteString(key, valueString);
            }

            WritePortBindingEnvironmentVariables(resource);

            Writer.WriteEndObject();
        }
    }

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public async Task WriteCommandLineArgumentsAsync(IResource resource)
    {
        var args = new List<object>();

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallback))
        {
            var commandLineArgsContext = new CommandLineArgsCallbackContext(args, CancellationToken);

            foreach (var callback in argsCallback)
            {
                await callback.Callback(commandLineArgsContext).ConfigureAwait(false);
            }
        }

        if (args.Count > 0)
        {
            Writer.WriteStartArray("args");

            foreach (var arg in args)
            {
                var valueString = arg switch
                {
                    string stringValue => stringValue,
                    IManifestExpressionProvider manifestExpression => manifestExpression.ValueExpression,
                    _ => throw new DistributedApplicationException($"The value of the argument '{arg}' is not supported.")
                };

                Writer.WriteStringValue(valueString);
            }

            Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Writes the "inputs" annotations for the underlying resource.
    /// </summary>
    /// <param name="resource">The resource to write inputs for.</param>
    public void WriteInputs(IResource resource)
    {
        if (resource.TryGetAnnotationsOfType<InputAnnotation>(out var inputs))
        {
            Writer.WriteStartObject("inputs");
            foreach (var input in inputs)
            {
                Writer.WriteStartObject(input.Name);

                // https://github.com/Azure/azure-dev/issues/3487 tracks being able to remove this. All inputs are strings.
                Writer.WriteString("type", "string");

                if (input.Secret)
                {
                    Writer.WriteBoolean("secret", true);
                }

                if (input.Default is not null)
                {
                    Writer.WriteStartObject("default");
                    input.Default.WriteToManifest(this);
                    Writer.WriteEndObject();
                }

                Writer.WriteEndObject();
            }
            Writer.WriteEndObject();
        }
    }

    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <param name="resource"></param>
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

    internal void WriteDockerBuildArgs(IEnumerable<DockerBuildArg>? buildArgs)
    {
        if (buildArgs?.ToArray() is { Length: > 0 } args)
        {
            Writer.WriteStartObject("buildArgs");

            for (var i = 0; i < args.Length; i++)
            {
                var buildArg = args[i];

                var valueString = buildArg.Value switch
                {
                    string stringValue => stringValue,
                    IManifestExpressionProvider manifestExpression => manifestExpression.ValueExpression,
                    null => null, // null means let docker build pull from env var.
                    _ => buildArg.Value.ToString()
                };

                Writer.WriteString(buildArg.Name, valueString);
            }

            Writer.WriteEndObject();
        }
    }

    internal void WriteManifestMetadata(IResource resource)
    {
        if (!resource.TryGetAnnotationsOfType<ManifestMetadataAnnotation>(out var metadataAnnotations))
        {
            return;
        }

        Writer.WriteStartObject("metadata");

        foreach (var metadataAnnotation in metadataAnnotations)
        {
            Writer.WritePropertyName(metadataAnnotation.Name);
            JsonSerializer.Serialize(Writer, metadataAnnotation.Value);
        }

        Writer.WriteEndObject();
    }

    private void WriteContainerMounts(ContainerResource container)
    {
        if (container.TryGetAnnotationsOfType<ContainerMountAnnotation>(out var mounts))
        {
            // Write out details for bind mounts
            var bindMounts = mounts.Where(mounts => mounts.Type == ContainerMountType.BindMount).ToList();
            if (bindMounts.Count > 0)
            {
                // Bind mounts are written as an array of objects to be consistent with volumes
                Writer.WriteStartArray("bindMounts");

                foreach (var bindMount in bindMounts)
                {
                    Writer.WriteStartObject();

                    Writer.WritePropertyName("source");
                    var manifestRelativeSource = GetManifestRelativePath(bindMount.Source);
                    Writer.WriteStringValue(manifestRelativeSource);

                    Writer.WritePropertyName("target");
                    Writer.WriteStringValue(bindMount.Target.Replace('\\', '/'));

                    Writer.WriteBoolean("readOnly", bindMount.IsReadOnly);

                    Writer.WriteEndObject();
                }

                Writer.WriteEndArray();
            }

            // Write out details for volumes
            var volumes = mounts.Where(mounts => mounts.Type == ContainerMountType.Volume).ToList();
            if (volumes.Count > 0)
            {
                // Volumes are written as an array of objects as anonymous volumes do not have a name
                Writer.WriteStartArray("volumes");

                foreach (var volume in volumes)
                {
                    Writer.WriteStartObject();

                    // This can be null for anonymous volumes
                    if (volume.Source is not null)
                    {
                        Writer.WritePropertyName("name");
                        Writer.WriteStringValue(volume.Source);
                    }

                    Writer.WritePropertyName("target");
                    Writer.WriteStringValue(volume.Target);

                    Writer.WriteBoolean("readOnly", volume.IsReadOnly);

                    Writer.WriteEndObject();
                }

                Writer.WriteEndArray();
            }
        }
    }
}
