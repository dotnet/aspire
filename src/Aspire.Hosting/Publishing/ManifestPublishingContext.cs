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

    private PortAllocator PortAllocator { get; } = new();

    /// <summary>
    /// Gets cancellation token for this operation.
    /// </summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    private readonly Dictionary<string, IResource> _referencedResources = [];

    private readonly HashSet<object?> _currentDependencySet = [];

    /// <summary>
    /// Generates a relative path based on the location of the manifest path.
    /// </summary>
    /// <param name="path">A path to a file.</param>
    /// <returns>The specified path as a relative path to the manifest.</returns>
    /// <exception cref="DistributedApplicationException">Throws when could not get the directory directory name from the output path.</exception>
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

    internal async Task WriteModel(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        Writer.WriteStartObject();
        Writer.WriteStartObject("resources");

        foreach (var resource in model.Resources)
        {
            await WriteResourceAsync(resource).ConfigureAwait(false);
        }

        await WriteReferencedResources(model).ConfigureAwait(false);

        Writer.WriteEndObject();
        Writer.WriteEndObject();

        await Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task WriteResourceAsync(IResource resource)
    {
        // First see if the resource has a callback annotation with overrides the behavior for rendering
        // out the JSON. If so use that callback, otherwise use the fallback logic that we have.
        if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation))
        {
            if (manifestPublishingCallbackAnnotation.Callback != null)
            {
                await WriteResourceObjectAsync(resource, () => manifestPublishingCallbackAnnotation.Callback(this)).ConfigureAwait(false);
            }
        }
        else if (resource is ContainerResource container)
        {
            await WriteResourceObjectAsync(container, () => WriteContainerAsync(container)).ConfigureAwait(false);
        }
        else if (resource is ProjectResource project)
        {
            await WriteResourceObjectAsync(project, () => WriteProjectAsync(project)).ConfigureAwait(false);
        }
        else if (resource is ExecutableResource executable)
        {
            await WriteResourceObjectAsync(executable, () => WriteExecutableAsync(executable)).ConfigureAwait(false);
        }
        else if (resource is IResourceWithConnectionString resourceWithConnectionString)
        {
            await WriteResourceObjectAsync(resource, () => WriteConnectionStringAsync(resourceWithConnectionString)).ConfigureAwait(false);
        }
        else if (resource is ParameterResource parameter)
        {
            await WriteResourceObjectAsync(parameter, () => WriteParameterAsync(parameter)).ConfigureAwait(false);
        }
        else
        {
            await WriteResourceObjectAsync(resource, WriteErrorAsync).ConfigureAwait(false);
        }

        async Task WriteResourceObjectAsync<T>(T resource, Func<Task> action) where T : IResource
        {
            Writer.WriteStartObject(resource.Name);
            await action().ConfigureAwait(false);
            Writer.WriteEndObject();
        }
    }

    private Task WriteErrorAsync()
    {
        Writer.WriteString("error", "This resource does not support generation in the manifest.");
        return Task.CompletedTask;
    }

    private Task WriteConnectionStringAsync(IResourceWithConnectionString resource)
    {
        // Write connection strings as value.v0
        Writer.WriteString("type", "value.v0");
        WriteConnectionString(resource);

        return Task.CompletedTask;
    }

    private async Task WriteProjectAsync(ProjectResource project)
    {
        Writer.WriteString("type", "project.v0");

        if (!project.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Project metadata not found.");
        }

        var relativePathToProjectFile = GetManifestRelativePath(metadata.ProjectPath);

        Writer.WriteString("path", relativePathToProjectFile);

        await WriteCommandLineArgumentsAsync(project).ConfigureAwait(false);

        await WriteEnvironmentVariablesAsync(project).ConfigureAwait(false);
        WriteBindings(project);
    }

    private async Task WriteExecutableAsync(ExecutableResource executable)
    {
        Writer.WriteString("type", "executable.v0");

        // Write the connection string if it exists.
        WriteConnectionString(executable);

        var relativePathToProjectFile = GetManifestRelativePath(executable.WorkingDirectory);

        Writer.WriteString("workingDirectory", relativePathToProjectFile);

        Writer.WriteString("command", executable.Command);

        await WriteCommandLineArgumentsAsync(executable).ConfigureAwait(false);

        await WriteEnvironmentVariablesAsync(executable).ConfigureAwait(false);
        WriteBindings(executable);
    }

    internal Task WriteParameterAsync(ParameterResource parameter)
    {
        Writer.WriteString("type", "parameter.v0");

        if (parameter.IsConnectionString)
        {
            Writer.WriteString("connectionString", parameter.ValueExpression);
        }

        Writer.WriteString("value", $"{{{parameter.Name}.inputs.value}}");

        Writer.WriteStartObject("inputs");
        Writer.WriteStartObject("value");

        // https://github.com/Azure/azure-dev/issues/3487 tracks being able to remove this. All inputs are strings.
        Writer.WriteString("type", "string");

        if (parameter.Secret)
        {
            Writer.WriteBoolean("secret", true);
        }

        if (parameter.Default is not null)
        {
            Writer.WriteStartObject("default");
            parameter.Default.WriteToManifest(this);
            Writer.WriteEndObject();
        }

        Writer.WriteEndObject();
        Writer.WriteEndObject();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes JSON elements to the manifest which represent a container resource.
    /// </summary>
    /// <param name="container">The container resource to written to the manifest.</param>
    /// <exception cref="DistributedApplicationException">Thrown if the container resource does not contain a <see cref="ContainerImageAnnotation"/>.</exception>
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
        WriteBindings(container);
    }

    /// <summary>
    /// Writes the "connectionString" field for the underlying resource.
    /// </summary>
    /// <param name="resource">The <see cref="IResource"/>.</param>
    public void WriteConnectionString(IResource resource)
    {
        if (resource is IResourceWithConnectionString resourceWithConnectionString &&
            resourceWithConnectionString.ConnectionStringExpression is { } connectionString)
        {
            Writer.WriteString("connectionString", connectionString.ValueExpression);
        }
    }

    /// <summary>
    /// Writes endpoints to the resource entry in the manifest based on the resource's
    /// <see cref="EndpointAnnotation"/> entries in the <see cref="IResource.Annotations"/>
    /// collection.
    /// </summary>
    /// <param name="resource">The <see cref="IResource"/> that contains <see cref="EndpointAnnotation"/> annotations.</param>
    public void WriteBindings(IResource resource)
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

                int? targetPort = (resource, endpoint.UriScheme, endpoint.TargetPort, endpoint.Port) switch
                {
                    // The port was specified so use it
                    (_, _, int target, _) => target,

                    // Container resources get their default listening port from the exposed port.
                    (ContainerResource, _, null, int port) => port,

                    // Project resources get their default listening port from the deployment tool
                    // ideally we would default to a known port but we don't know it at this point
                    (ProjectResource, var scheme, null, _) when scheme is "http" or "https" => null,

                    // Allocate a dynamic port
                    _ => PortAllocator.AllocatePort()
                };

                int? exposedPort = (endpoint.UriScheme, endpoint.Port, targetPort) switch
                {
                    // Exposed port and target port are the same, we don't need to mention the exposed port
                    (_, int p0, int p1) when p0 == p1 => null,

                    // Port was specified, so use it
                    (_, int port, _) => port,

                    // We have a target port, not need to specify an exposedPort
                    // it will default to the targetPort
                    (_, null, int port) => null,

                    // Let the tool infer the default http and https ports
                    ("http", null, null) => null,
                    ("https", null, null) => null,

                    // Other schemes just allocate a port
                    _ => PortAllocator.AllocatePort()
                };

                if (exposedPort is int ep)
                {
                    PortAllocator.AddUsedPort(ep);
                    Writer.WriteNumber("port", ep);
                }

                if (targetPort is int tp)
                {
                    PortAllocator.AddUsedPort(tp);
                    Writer.WriteNumber("targetPort", tp);
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
    /// Writes environment variables to the manifest base on the <see cref="IResource"/> resource's <see cref="EnvironmentCallbackAnnotation"/> annotations."/>
    /// </summary>
    /// <param name="resource">The <see cref="IResource"/> which contains <see cref="EnvironmentCallbackAnnotation"/> annotations.</param>
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

                TryAddDependentResources(value);
            }

            Writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Writes command line arguments to the manifest based on the <see cref="IResource"/> resource's <see cref="CommandLineArgsCallbackAnnotation"/> annotations.
    /// </summary>
    /// <param name="resource">The <see cref="IResource"/> that contains <see cref="CommandLineArgsCallbackAnnotation"/> annotations.</param>
    /// <returns>The <see cref="Task"/> to await for completion.</returns>
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

                TryAddDependentResources(arg);
            }

            Writer.WriteEndArray();
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
                    bool boolValue => boolValue ? "true" : "false",
                    null => null, // null means let docker build pull from env var.
                    _ => buildArg.Value.ToString()
                };

                Writer.WriteString(buildArg.Name, valueString);

                TryAddDependentResources(buildArg.Value);
            }

            Writer.WriteEndObject();
        }
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

    /// <summary>
    /// Ensures that any <see cref="IResource"/> instances referenced by <paramref name="value"/> are
    /// written to the manifest.
    /// </summary>
    /// <param name="value">The object to check for references that may be resources that need to be written.</param>
    public void TryAddDependentResources(object? value)
    {
        if (value is IResource resource)
        {
            // add the resource to the ReferencedResources for now. After the whole model is written,
            // these will be written to the manifest
            _referencedResources.TryAdd(resource.Name, resource);
        }
        else if (value is IValueWithReferences objectWithReferences)
        {
            // ensure we don't infinitely recurse if there are cycles in the graph
            _currentDependencySet.Add(value);
            foreach (var dependency in objectWithReferences.References)
            {
                if (!_currentDependencySet.Contains(dependency))
                {
                    TryAddDependentResources(dependency);
                }
            }
            _currentDependencySet.Remove(value);
        }
    }

    private async Task WriteReferencedResources(DistributedApplicationModel model)
    {
        // remove references that were already in the model and were already written
        foreach (var existingResource in model.Resources)
        {
            _referencedResources.Remove(existingResource.Name);
        }

        // now write all the leftover referenced resources
        foreach (var resource in _referencedResources.Values)
        {
            await WriteResourceAsync(resource).ConfigureAwait(false);
        }

        _referencedResources.Clear();
    }
}
