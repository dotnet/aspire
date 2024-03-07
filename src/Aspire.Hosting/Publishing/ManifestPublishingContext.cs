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
        var relativePath = Path.GetRelativePath(manifestDirectory, path);

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

        if (container.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallback))
        {
            var args = new List<string>();

            var commandLineArgsContext = new CommandLineArgsCallbackContext(args, CancellationToken);

            foreach (var callback in argsCallback)
            {
                await callback.Callback(commandLineArgsContext).ConfigureAwait(false);
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
            resourceWithConnectionString.ConnectionStringExpression is string connectionString)
        {
            Writer.WriteString("connectionString", connectionString);
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

            WriteServiceDiscoveryEnvironmentVariables(resource);

            WritePortBindingEnvironmentVariables(resource);

            Writer.WriteEndObject();
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
                Writer.WriteString("type", input.Type);

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
                    .Where(sba => endpointNames.Contains(sba.Name, StringComparers.EndpointAnnotationName))
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
}
