// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging.Abstractions;

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
        Writer.WriteString("$schema", SchemaUtils.SchemaVersion);
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
        else if (resource is ParameterResource parameter)
        {
            await WriteResourceObjectAsync(parameter, () => WriteParameterAsync(parameter)).ConfigureAwait(false);
        }
        else if (resource is IResourceWithConnectionString resourceWithConnectionString)
        {
            await WriteResourceObjectAsync(resource, () => WriteConnectionStringAsync(resourceWithConnectionString)).ConfigureAwait(false);
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
        if (!project.TryGetLastAnnotation<IProjectMetadata>(out var metadata))
        {
            throw new DistributedApplicationException("Project metadata not found.");
        }

        var relativePathToProjectFile = GetManifestRelativePath(metadata.ProjectPath);

        var deploymentTarget = project.GetDeploymentTargetAnnotation();
        if (deploymentTarget is not null)
        {
            Writer.WriteString("type", "project.v1");
        }
        else
        {
            Writer.WriteString("type", "project.v0");
        }

        Writer.WriteString("path", relativePathToProjectFile);

        if (deploymentTarget is not null)
        {
            await WriteDeploymentTarget(deploymentTarget).ConfigureAwait(false);
        }

        await WriteCommandLineArgumentsAsync(project).ConfigureAwait(false);

        await WriteEnvironmentVariablesAsync(project).ConfigureAwait(false);

        WriteBindings(project);
    }

    private async Task WriteDeploymentTarget(DeploymentTargetAnnotation deploymentTarget)
    {
        if (deploymentTarget.DeploymentTarget.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var manifestPublishingCallbackAnnotation) &&
            manifestPublishingCallbackAnnotation.Callback is not null)
        {
            Writer.WriteStartObject("deployment");
            await manifestPublishingCallbackAnnotation.Callback(this).ConfigureAwait(false);
            Writer.WriteEndObject();
        }
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
        var deploymentTarget = container.GetDeploymentTargetAnnotation();

        if (container.Annotations.OfType<DockerfileBuildAnnotation>().Any())
        {
            Writer.WriteString("type", "container.v1");
            WriteConnectionString(container);
            WriteBuildContext(container);
        }
        else
        {
            if (!container.TryGetContainerImageName(out var image))
            {
                throw new DistributedApplicationException("Could not get container image name.");
            }

            if (deploymentTarget is not null)
            {
                Writer.WriteString("type", "container.v1");
            }
            else
            {
                Writer.WriteString("type", "container.v0");
            }

            WriteConnectionString(container);
            Writer.WriteString("image", image);
        }

        if (deploymentTarget is not null)
        {
            await WriteDeploymentTarget(deploymentTarget).ConfigureAwait(false);
        }

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

    private void WriteBuildContext(ContainerResource container)
    {
        if (container.TryGetAnnotationsOfType<DockerfileBuildAnnotation>(out var annotations) && annotations.Single() is { } annotation)
        {
            Writer.WriteStartObject("build");
            Writer.WriteString("context", GetManifestRelativePath(annotation.ContextPath));
            Writer.WriteString("dockerfile", GetManifestRelativePath(annotation.DockerfilePath));

            if (annotation.Stage is { } stage)
            {
                Writer.WriteString("stage", stage);
            }

            if (annotation.BuildArguments.Count > 0)
            {
                Writer.WriteStartObject("args");

                foreach (var (key, value) in annotation.BuildArguments)
                {
                    var valueString = value switch
                    {
                        string stringValue => stringValue,
                        IManifestExpressionProvider manifestExpression => manifestExpression.ValueExpression,
                        bool boolValue => boolValue ? "true" : "false",
                        null => null, // null means let docker build pull from env var.
                        _ => value.ToString()
                    };

                    Writer.WriteString(key, valueString);
                }

                Writer.WriteEndObject();
            }

            if (annotation.BuildSecrets.Count > 0)
            {
                Writer.WriteStartObject("secrets");

                foreach (var (key, value) in annotation.BuildSecrets)
                {
                    var valueString = value switch
                    {
                        FileInfo fileValue => GetManifestRelativePath(fileValue.FullName),
                        string stringValue => stringValue,
                        IManifestExpressionProvider manifestExpression => manifestExpression.ValueExpression,
                        bool boolValue => boolValue ? "true" : "false",
                        null => null, // null means let docker build pull from env var.
                        _ => value.ToString()
                    };

                    Writer.WriteStartObject(key);

                    if (value is FileInfo)
                    {
                        Writer.WriteString("type", "file");
                        Writer.WriteString("source", valueString);
                    }
                    else
                    {
                        Writer.WriteString("type", "env");
                        Writer.WriteString("value", valueString);
                    }

                    Writer.WriteEndObject();
                }

                Writer.WriteEndObject();
            }

            Writer.WriteEndObject();
        }
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
            // This is used to determine if an endpoint should be treated as the Default endpoint.
            // Endpoints can come from 3 different sources (in this order):
            // 1. Kestrel configuration
            // 2. Default endpoints added by the framework
            // 3. Explicitly added endpoints
            // But wherever they come from, we treat the first one as Default, for each scheme.
            var httpSchemesEncountered = new HashSet<string>();

            static bool IsHttpScheme(string scheme) => scheme is "http" or "https";

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

                    // Check whether the project view this endpoint as Default (for its scheme).
                    // If so, we don't specify the target port, as it will get one from the deployment tool.
                    (ProjectResource project, string uriScheme, null, _) when IsHttpScheme(uriScheme) && !httpSchemesEncountered.Contains(uriScheme) => null,

                    // Allocate a dynamic port
                    _ => PortAllocator.AllocatePort()
                };

                // We only keep track of schemes for project resources, since we don't want
                // a non-project scheme to affect what project endpoints are considered default.
                if (resource is ProjectResource && IsHttpScheme(endpoint.UriScheme))
                {
                    httpSchemesEncountered.Add(endpoint.UriScheme);
                }

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
        var env = new Dictionary<string, (object, string)>();

        await resource.ProcessEnvironmentVariableValuesAsync(
            ExecutionContext,
            (key, unprocessed, processed, ex) =>
            {
                if (ex is not null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }

                if (unprocessed is not null && processed is not null)
                {
                    env[key] = (unprocessed, processed);
                }
            },
            NullLogger.Instance,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        if (env.Count > 0)
        {
            Writer.WriteStartObject("env");

            foreach (var (key, value) in env)
            {
                var (unprocessed, processed) = value;

                Writer.WriteString(key, processed);

                TryAddDependentResources(unprocessed);
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
        var args = new List<(object, string)>();

        await resource.ProcessArgumentValuesAsync(
            ExecutionContext,
            (unprocessed, expression, ex, _) =>
            {
                if (ex is not null)
                {
                    ExceptionDispatchInfo.Throw(ex);
                }

                if (unprocessed is not null && expression is not null)
                {
                    args.Add((unprocessed, expression));
                }
            },
            NullLogger.Instance,
            cancellationToken: CancellationToken).ConfigureAwait(false);

        if (args.Count > 0)
        {
            Writer.WriteStartArray("args");

            foreach (var (unprocessed, expression) in args)
            {
                Writer.WriteStringValue(expression);

                TryAddDependentResources(unprocessed);
            }

            Writer.WriteEndArray();
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
