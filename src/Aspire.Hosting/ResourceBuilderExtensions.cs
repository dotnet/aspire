// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using HealthChecks.Uris;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring resources with environment variables.
/// </summary>
public static class ResourceBuilderExtensions
{
    private const string ConnectionStringEnvironmentName = "ConnectionStrings__";

    /// <summary>
    /// Adds an environment variable to the resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>A resource configured with the specified environment variable.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, string? value) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentAnnotation(name, value ?? string.Empty));
    }

    /// <summary>
    /// Adds an environment variable to the resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>A resource configured with the specified environment variable.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, in ReferenceExpression.ExpressionInterpolatedStringHandler value)
        where T : IResourceWithEnvironment
    {
        var expression = value.GetExpression();

        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[name] = expression;
        });
    }

    /// <summary>
    /// Adds an environment variable to the resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value of the environment variable.</param>
    /// <returns>A resource configured with the specified environment variable.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, ReferenceExpression value)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[name] = value;
        });
    }

    /// <summary>
    /// Adds an environment variable to the resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="callback">A callback that allows for deferred execution of a specific environment variable. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>A resource configured with the specified environment variable.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, Func<string> callback) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, callback));
    }

    /// <summary>
    /// Allows for the population of environment variables on a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing many environment variables. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, Action<EnvironmentCallbackContext> callback) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    /// <summary>
    /// Allows for the population of environment variables on a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing many environment variables. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, Func<EnvironmentCallbackContext, Task> callback) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    /// <summary>
    /// Adds an environment variable to the resource with the endpoint for <paramref name="endpointReference"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="endpointReference">The endpoint from which to extract the url.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, EndpointReference endpointReference) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[name] = endpointReference;
        });
    }

    /// <summary>
    /// Adds an environment variable to the resource with the value from <paramref name="parameter"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">Name of environment variable</param>
    /// <param name="parameter">Resource builder for the parameter resource.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, IResourceBuilder<ParameterResource> parameter) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[name] = parameter.Resource;
        });
    }

    /// <summary>
    /// Adds an environment variable to the resource with the connection string from the referenced resource.
    /// </summary>
    /// <typeparam name="T">The destination resource type.</typeparam>
    /// <param name="builder">The destination resource builder to which the environment variable will be added.</param>
    /// <param name="envVarName">The name of the environment variable under which the connection string will be set.</param>
    /// <param name="resource">The resource builder of the referenced service from which to pull the connection string.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(
        this IResourceBuilder<T> builder,
        string envVarName,
        IResourceBuilder<IResourceWithConnectionString> resource)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[envVarName] = new ConnectionStringReference(resource.Resource, optional: false);
        });
    }

    /// <summary>
    /// Adds the arguments to be passed to a container resource when the container is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to be passed to the container when it is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, params string[] args) where T : IResourceWithArgs
    {
        return builder.WithArgs(context => context.Args.AddRange(args));
    }

    /// <summary>
    /// Adds the arguments to be passed to a container resource when the container is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="args">The arguments to be passed to the container when it is started.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, params object[] args) where T : IResourceWithArgs
    {
        return builder.WithArgs(context => context.Args.AddRange(args));
    }

    /// <summary>
    /// Adds a callback to be executed with a list of command-line arguments when a container resource is started.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, Action<CommandLineArgsCallbackContext> callback) where T : IResourceWithArgs
    {
        return builder.WithArgs(context =>
        {
            callback(context);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Adds a callback to be executed with a list of command-line arguments when a container resource is started.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">A callback that allows for deferred execution for computing arguments. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connection strings, ports.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithArgs<T>(this IResourceBuilder<T> builder, Func<CommandLineArgsCallbackContext, Task> callback) where T : IResourceWithArgs
    {
        return builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(callback));
    }

    /// <summary>
    /// Registers a callback which is invoked when manifest is generated for the app model.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">Callback method which takes a <see cref="ManifestPublishingContext"/> which can be used to inject JSON into the manifest.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithManifestPublishingCallback<T>(this IResourceBuilder<T> builder, Action<ManifestPublishingContext> callback) where T : IResource
    {
        // You can only ever have one manifest publishing callback, so it must be a replace operation.
        return builder.WithAnnotation(new ManifestPublishingCallbackAnnotation(callback), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Registers an async callback which is invoked when manifest is generated for the app model.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">Callback method which takes a <see cref="ManifestPublishingContext"/> which can be used to inject JSON into the manifest.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithManifestPublishingCallback<T>(this IResourceBuilder<T> builder, Func<ManifestPublishingContext, Task> callback) where T : IResource
    {
        // You can only ever have one manifest publishing callback, so it must be a replace operation.
        return builder.WithAnnotation(new ManifestPublishingCallbackAnnotation(callback), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Registers a callback which is invoked when a connection string is requested for a resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="resource">Resource to which connection string generation is redirected.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithConnectionStringRedirection<T>(this IResourceBuilder<T> builder, IResourceWithConnectionString resource) where T : IResourceWithConnectionString
    {
        // You can only ever have one manifest publishing callback, so it must be a replace operation.
        return builder.WithAnnotation(new ConnectionStringRedirectAnnotation(resource), ResourceAnnotationMutationBehavior.Replace);
    }

    private static Action<EnvironmentCallbackContext> CreateEndpointReferenceEnvironmentPopulationCallback(EndpointReferenceAnnotation endpointReferencesAnnotation)
    {
        return (context) =>
        {
            var annotation = endpointReferencesAnnotation;
            var serviceName = annotation.Resource.Name;
            foreach (var endpoint in annotation.Resource.GetEndpoints())
            {
                var endpointName = endpoint.EndpointName;
                if (!annotation.UseAllEndpoints && !annotation.EndpointNames.Contains(endpointName))
                {
                    // Skip this endpoint since it's not in the list of endpoints we want to reference.
                    continue;
                }

                // Add the endpoint, rewriting localhost to the container host if necessary.
                context.EnvironmentVariables[$"services__{serviceName}__{endpointName}__0"] = endpoint;
            }
        };
    }

    /// <summary>
    /// Injects a connection string as an environment variable from the source resource into the destination resource, using the source resource's name as the connection string name (if not overridden).
    /// The format of the environment variable will be "ConnectionStrings__{sourceResourceName}={connectionString}."
    /// <para>
    /// Each resource defines the format of the connection string value. The
    /// underlying connection string value can be retrieved using <see cref="IResourceWithConnectionString.GetConnectionStringAsync(CancellationToken)"/>.
    /// </para>
    /// <para>
    /// Connection strings are also resolved by the configuration system (appSettings.json in the AppHost project, or environment variables). If a connection string is not found on the resource, the configuration system will be queried for a connection string
    /// using the resource's name.
    /// </para>
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where connection string will be injected.</param>
    /// <param name="source">The resource from which to extract the connection string.</param>
    /// <param name="connectionName">An override of the source resource's name for the connection string. The resulting connection string will be "ConnectionStrings__connectionName" if this is not null.</param>
    /// <param name="optional"><see langword="true"/> to allow a missing connection string; <see langword="false"/> to throw an exception if the connection string is not found.</param>
    /// <exception cref="DistributedApplicationException">Throws an exception if the connection string resolves to null. It can be null if the resource has no connection string, and if the configuration has no connection string for the source resource.</exception>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithConnectionString> source, string? connectionName = null, bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        var resource = source.Resource;
        connectionName ??= resource.Name;

        return builder.WithEnvironment(context =>
        {
            var connectionStringName = resource.ConnectionStringEnvironmentVariable ?? $"{ConnectionStringEnvironmentName}{connectionName}";

            context.EnvironmentVariables[connectionStringName] = new ConnectionStringReference(resource, optional)
            {
                ConnectionName = connectionName
            };
        });
    }

    /// <summary>
    /// Injects service discovery information as environment variables from the project resource into the destination resource, using the source resource's name as the service name.
    /// Each endpoint defined on the project resource will be injected using the format "services__{sourceResourceName}__{endpointName}__{endpointIndex}={uriString}."
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="source">The resource from which to extract service discovery information.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithServiceDiscovery> source)
        where TDestination : IResourceWithEnvironment
    {
        ApplyEndpoints(builder, source.Resource);
        return builder;
    }

    /// <summary>
    /// Injects service discovery information as environment variables from the uri into the destination resource, using the name as the service name.
    /// The uri will be injected using the format "services__{name}__default__0={uri}."
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="name">The name of the service.</param>
    /// <param name="uri">The uri of the service.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, string name, Uri uri)
        where TDestination : IResourceWithEnvironment
    {
        if (!uri.IsAbsoluteUri)
        {
            throw new InvalidOperationException("The uri for service reference must be absolute.");
        }

        if (uri.AbsolutePath != "/")
        {
            throw new InvalidOperationException("The uri absolute path must be \"/\".");
        }

        return builder.WithEnvironment($"services__{name}__default__0", uri.ToString());
    }

    /// <summary>
    /// Injects service discovery information from the specified endpoint into the project resource using the source resource's name as the service name.
    /// Each endpoint will be injected using the format "services__{sourceResourceName}__{endpointName}__{endpointIndex}={uriString}."
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="endpointReference">The endpoint from which to extract the url.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, EndpointReference endpointReference)
        where TDestination : IResourceWithEnvironment
    {
        ApplyEndpoints(builder, endpointReference.Resource, endpointReference.EndpointName);
        return builder;
    }

    private static void ApplyEndpoints<T>(this IResourceBuilder<T> builder, IResourceWithEndpoints resourceWithEndpoints, string? endpointName = null)
        where T : IResourceWithEnvironment
    {
        // When adding an endpoint we get to see whether there is an EndpointReferenceAnnotation
        // on the resource, if there is then it means we have already been here before and we can just
        // skip this and note the endpoint that we want to apply to the environment in the future
        // in a single pass. There is one EndpointReferenceAnnotation per endpoint source.
        var endpointReferenceAnnotation = builder.Resource.Annotations
            .OfType<EndpointReferenceAnnotation>()
            .Where(sra => sra.Resource == resourceWithEndpoints)
            .SingleOrDefault();

        if (endpointReferenceAnnotation == null)
        {
            endpointReferenceAnnotation = new EndpointReferenceAnnotation(resourceWithEndpoints);
            builder.WithAnnotation(endpointReferenceAnnotation);

            var callback = CreateEndpointReferenceEnvironmentPopulationCallback(endpointReferenceAnnotation);
            builder.WithEnvironment(callback);
        }

        // If no specific endpoint name is specified, go and add all the endpoints.
        if (endpointName == null)
        {
            endpointReferenceAnnotation.UseAllEndpoints = true;
        }
        else
        {
            endpointReferenceAnnotation.EndpointNames.Add(endpointName);
        }
    }

    /// <summary>
    /// Changes an existing creates a new endpoint if it doesn't exist and invokes callback to modify the defaults.
    /// </summary>
    /// <param name="builder">Resource builder for resource with endpoints.</param>
    /// <param name="endpointName">Name of endpoint to change.</param>
    /// <param name="callback">Callback that modifies the endpoint.</param>
    /// <param name="createIfNotExists">Create endpoint if it does not exist.</param>
    /// <returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "<Pending>")]
    public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, [EndpointName] string endpointName, Action<EndpointAnnotation> callback, bool createIfNotExists = true) where T : IResourceWithEndpoints
    {
        var endpoint = builder.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Where(ea => StringComparers.EndpointAnnotationName.Equals(ea.Name, endpointName))
            .SingleOrDefault();

        if (endpoint != null)
        {
            callback(endpoint);
        }

        if (endpoint == null && createIfNotExists)
        {
            endpoint = new EndpointAnnotation(ProtocolType.Tcp, name: endpointName);
            callback(endpoint);
            builder.Resource.Annotations.Add(endpoint);
        }
        else if (endpoint == null && !createIfNotExists)
        {
            return builder;
        }

        return builder;
    }

    /// <summary>
    /// Exposes an endpoint on a resource. This endpoint reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The endpoint name will be the scheme name if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetPort">This is the port the resource is listening on. If the endpoint is used for the container, it is the container port.</param>
    /// <param name="port">An optional port. This is the port that will be given to other resource to communicate with this resource.</param>
    /// <param name="scheme">An optional scheme e.g. (http/https). Defaults to "tcp" if not specified.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to the scheme name if not specified.</param>
    /// <param name="env">An optional name of the environment variable that will be used to inject the <paramref name="targetPort"/>. If the target port is null one will be dynamically generated and assigned to the environment variable.</param>
    /// <param name="isExternal">Indicates that this endpoint should be exposed externally at publish time.</param>
    /// <param name="isProxied">Specifies if the endpoint will be proxied by DCP. Defaults to true.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="DistributedApplicationException">Throws an exception if an endpoint with the same name already exists on the specified resource.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "<Pending>")]
    public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int? port = null, int? targetPort = null, string? scheme = null, [EndpointName] string? name = null, string? env = null, bool isProxied = true, bool? isExternal = null) where T : IResourceWithEndpoints
    {
        var annotation = new EndpointAnnotation(
            protocol: ProtocolType.Tcp,
            uriScheme: scheme,
            name: name,
            port: port,
            targetPort: targetPort,
            isExternal: isExternal,
            isProxied: isProxied);

        if (builder.Resource.Annotations.OfType<EndpointAnnotation>().Any(sb => string.Equals(sb.Name, annotation.Name, StringComparisons.EndpointAnnotationName)))
        {
            throw new DistributedApplicationException($"Endpoint with name '{annotation.Name}' already exists. Endpoint name may not have been explicitly specified and was derived automatically from scheme argument (e.g. 'http', 'https', or 'tcp'). Multiple calls to WithEndpoint (and related methods) may result in a conflict if name argument is not specified. Each endpoint must have a unique name. For more information on networking in .NET Aspire see: https://aka.ms/dotnet/aspire/networking");
        }

        // Set the environment variable on the resource
        if (env is not null && builder.Resource is IResourceWithEndpoints resourceWithEndpoints and IResourceWithEnvironment)
        {
            annotation.TargetPortEnvironmentVariable = env;

            var endpointReference = new EndpointReference(resourceWithEndpoints, annotation);

            builder.WithAnnotation(new EnvironmentCallbackAnnotation(context =>
            {
                context.EnvironmentVariables[env] = endpointReference.Property(EndpointProperty.TargetPort);
            }));
        }

        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Exposes an HTTP endpoint on a resource. This endpoint reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The endpoint name will be "http" if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetPort">This is the port the resource is listening on. If the endpoint is used for the container, it is the container port.</param>
    /// <param name="port">An optional port. This is the port that will be given to other resource to communicate with this resource.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to "http" if not specified.</param>
    /// <param name="env">An optional name of the environment variable to inject.</param>
    /// <param name="isProxied">Specifies if the endpoint will be proxied by DCP. Defaults to true.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="DistributedApplicationException">Throws an exception if an endpoint with the same name already exists on the specified resource.</exception>
    public static IResourceBuilder<T> WithHttpEndpoint<T>(this IResourceBuilder<T> builder, int? port = null, int? targetPort = null, [EndpointName] string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints
    {
        return builder.WithEndpoint(targetPort: targetPort, port: port, scheme: "http", name: name, env: env, isProxied: isProxied);
    }

    /// <summary>
    /// Exposes an HTTPS endpoint on a resource. This endpoint reference can be retrieved using <see cref="ResourceBuilderExtensions.GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The endpoint name will be "https" if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="targetPort">This is the port the resource is listening on. If the endpoint is used for the container, it is the container port.</param>
    /// <param name="port">An optional host port.</param>
    /// <param name="name">An optional name of the endpoint. Defaults to "https" if not specified.</param>
    /// <param name="env">An optional name of the environment variable to inject.</param>
    /// <param name="isProxied">Specifies if the endpoint will be proxied by DCP. Defaults to true.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="DistributedApplicationException">Throws an exception if an endpoint with the same name already exists on the specified resource.</exception>
    public static IResourceBuilder<T> WithHttpsEndpoint<T>(this IResourceBuilder<T> builder, int? port = null, int? targetPort = null, [EndpointName] string? name = null, string? env = null, bool isProxied = true) where T : IResourceWithEndpoints
    {
        return builder.WithEndpoint(targetPort: targetPort, port: port, scheme: "https", name: name, env: env, isProxied: isProxied);
    }

    /// <summary>
    /// Marks existing http or https endpoints on a resource as external.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithExternalHttpEndpoints<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        if (!builder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpoints))
        {
            return builder;
        }

        foreach (var endpoint in endpoints)
        {
            if (endpoint.UriScheme == "http" || endpoint.UriScheme == "https")
            {
                endpoint.IsExternal = true;
            }
        }

        return builder;
    }

    /// <summary>
    /// Gets an <see cref="EndpointReference"/> by name from the resource. These endpoints are declared either using <see cref="WithEndpoint{T}(IResourceBuilder{T}, int?, int?, string?, string?, string?, bool, bool?)"/> or by launch settings (for project resources).
    /// The <see cref="EndpointReference"/> can be used to resolve the address of the endpoint in <see cref="WithEnvironment{T}(IResourceBuilder{T}, Action{EnvironmentCallbackContext})"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The the resource builder.</param>
    /// <param name="name">The name of the endpoint.</param>
    /// <returns>An <see cref="EndpointReference"/> that can be used to resolve the address of the endpoint after resource allocation has occurred.</returns>
    public static EndpointReference GetEndpoint<T>(this IResourceBuilder<T> builder, [EndpointName] string name) where T : IResourceWithEndpoints
    {
        return builder.Resource.GetEndpoint(name);
    }

    /// <summary>
    /// Configures a resource to mark all endpoints' transport as HTTP/2. This is useful for HTTP/2 services that need prior knowledge.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> AsHttp2Service<T>(this IResourceBuilder<T> builder) where T : IResourceWithEndpoints
    {
        return builder.WithAnnotation(new Http2ServiceAnnotation());
    }

    /// <summary>
    /// Excludes a resource from being published to the manifest.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource to exclude.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> ExcludeFromManifest<T>(this IResourceBuilder<T> builder) where T : IResource
    {
        return builder.WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);
    }

    /// <summary>
    /// Waits for the dependency resource to enter the Running state before starting the resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder for the resource that will be waiting.</param>
    /// <param name="dependency">The resource builder for the dependency resource.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>This method is useful when a resource should wait until another has started running. This can help
    /// reduce errors in logs during local development where dependency resources.</para>
    /// <para>Some resources automatically register health checks with the application host container. For these
    /// resources, calling <see cref="WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/> also results
    /// in the resource being blocked from starting until the health checks associated with the dependency resource
    /// return <see cref="HealthStatus.Healthy"/>.</para>
    /// <para>The <see cref="WithHealthCheck{T}(IResourceBuilder{T}, string)"/> method can be used to associate
    /// additional health checks with a resource.</para>
    /// </remarks>
    /// <example>
    /// Start message queue before starting the worker service.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var messaging = builder.AddRabbitMQ("messaging");
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(messaging)
    ///        .WaitFor(messaging);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WaitFor<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency) where T : IResourceWithWaitSupport
    {
        if (builder.Resource as IResource == dependency.Resource)
        {
            throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource cannot wait for itself.");
        }

        if (builder.Resource is IResourceWithParent resourceWithParent && resourceWithParent.Parent == dependency.Resource)
        {
            throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource cannot wait for its parent '{dependency.Resource.Name}'.");
        }

        if (dependency.Resource is IResourceWithParent dependencyResourceWithParent)
        {
            // If the dependency resource is a child resource we automatically apply
            // the WaitFor to the parent resource. This caters for situations where
            // the child resource itself does not have any health checks setup.
            var parentBuilder = builder.ApplicationBuilder.CreateResourceBuilder(dependencyResourceWithParent.Parent);
            builder.WaitFor(parentBuilder);
        }

        return builder.WithAnnotation(new WaitAnnotation(dependency.Resource, WaitType.WaitUntilHealthy));
    }

    /// <summary>
    /// Waits for the dependency resource to enter the Exited or Finished state before starting the resource.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder for the resource that will be waiting.</param>
    /// <param name="dependency">The resource builder for the dependency resource.</param>
    /// <param name="exitCode">The exit code which is interpreted as successful.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>This method is useful when a resource should wait until another has completed. A common usage pattern
    /// would be to include a console application that initializes the database schema or performs other one off
    /// initialization tasks.</para>
    /// <para>Note that this method has no impact at deployment time and only works for local development.</para>
    /// </remarks>
    /// <example>
    /// Wait for database initialization app to complete running.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var pgsql = builder.AddPostgres("postgres");
    /// var dbprep = builder.AddProject&lt;Projects.DbPrepApp&gt;("dbprep")
    ///                     .WithReference(pgsql);
    /// builder.AddProject&lt;Projects.DatabasePrepTool&gt;("dbprep")
    ///        .WithReference(pgsql)
    ///        .WaitForCompletion(dbprep);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WaitForCompletion<T>(this IResourceBuilder<T> builder, IResourceBuilder<IResource> dependency, int exitCode = 0) where T : IResourceWithWaitSupport
    {
        if (builder.Resource as IResource == dependency.Resource)
        {
            throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource cannot wait for itself.");
        }

        if (builder.Resource is IResourceWithParent resourceWithParent && resourceWithParent.Parent == dependency.Resource)
        {
            throw new DistributedApplicationException($"The '{builder.Resource.Name}' resource cannot wait for its parent '{dependency.Resource.Name}'.");
        }

        return builder.WithAnnotation(new WaitAnnotation(dependency.Resource, WaitType.WaitForCompletion, exitCode));
    }

    /// <summary>
    /// Adds a <see cref="HealthCheckAnnotation"/> to the resource annotations to associate a resource with a named health check managed by the health check service.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="key">The key for the health check.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="WithHealthCheck{T}(IResourceBuilder{T}, string)"/> method is used in conjunction with
    /// the <see cref="WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/> to associate a resource
    /// registered in the application hosts dependency injection container. The <see cref="WithHealthCheck{T}(IResourceBuilder{T}, string)"/>
    /// method does not inject the health check itself it is purely an association mechanism.
    /// </para>
    /// </remarks>
    /// <example>
    /// Define a custom health check and associate it with a resource.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var startAfter = DateTime.Now.AddSeconds(30);
    /// 
    /// builder.Services.AddHealthChecks().AddCheck(mycheck", () =>
    /// {
    ///     return DateTime.Now > startAfter ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    /// });
    ///
    /// var pg = builder.AddPostgres("pg")
    ///                 .WithHealthCheck("mycheck");
    ///
    /// builder.AddProject&lt;Projects.MyApp&gt;("myapp")
    ///        .WithReference(pg)
    ///        .WaitFor(pg); // This will result in waiting for the building check, and the
    ///                      // custom check defined in the code.
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithHealthCheck<T>(this IResourceBuilder<T> builder, string key) where T : IResource
    {
        if (builder.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations) && annotations.Any(a => a.Key == key))
        {
            throw new DistributedApplicationException($"Resource '{builder.Resource.Name}' already has a health check with key '{key}'.");
        }

        builder.WithAnnotation(new HealthCheckAnnotation(key));

        return builder;
    }

    /// <summary>
    /// Adds a health check to the resource which is mapped to a specific endpoint.
    /// </summary>
    /// <typeparam name="T">A resource type that implements <see cref="IResourceWithEndpoints" />.</typeparam>
    /// <param name="builder">A resource builder.</param>
    /// <param name="path">The relative path to test.</param>
    /// <param name="statusCode">The result code to interpret as healthy.</param>
    /// <param name="endpointName">The name of the endpoint to derive the base address from.</param>
    /// <returns>A resource builder.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a health check to the health check service which polls the specified endpoint on the resource
    /// on a periodic basis. The base address is dynamically determined based on the endpoint that was selected. By
    /// default the path is set to "/" and the status code is set to 200.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example shows add a HTTP health check to a backend project
    /// to make sure that the front end does not start until the backend is
    /// reporting a healthy status based on the return code returned from the
    /// "/health" path on the backend server.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var backend = builder.AddProject&lt;Projects.Backend&gt;("backend")
    ///                      .WithHttpHealthCheck("/health");
    /// builder.AddProject&lt;Projects.Backend&gt;("backend")
    ///        .WithReference(backend).WaitFor(backend);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithHttpHealthCheck<T>(this IResourceBuilder<T> builder, string? path = null, int? statusCode = null, string? endpointName = null) where T : IResourceWithEndpoints
    {
        endpointName = endpointName ?? "http";
        return builder.WithHttpHealthCheckInternal(
            path: path,
            desiredScheme: "http",
            endpointName: endpointName,
            statusCode: statusCode
            );
    }

    internal static IResourceBuilder<T> WithHttpHealthCheckInternal<T>(this IResourceBuilder<T> builder, string desiredScheme, string endpointName, string? path = null, int? statusCode = null) where T : IResourceWithEndpoints
    {
        path = path ?? "/";
        statusCode = statusCode ?? 200;

        var endpoint = builder.Resource.GetEndpoint(endpointName);

        builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>((@event, ct) =>
        {
            if (!endpoint.Exists)
            {
                throw new DistributedApplicationException($"The endpoint '{endpointName}' does not exist on the resource '{builder.Resource.Name}'.");
            }

            if (endpoint.Scheme != desiredScheme)
            {
                throw new DistributedApplicationException($"The endpoint '{endpointName}' on resource '{builder.Resource.Name}' was not using the '{desiredScheme}' scheme.");
            }

            return Task.CompletedTask;
        });

        Uri? uri = null;
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(builder.Resource, (@event, ct) =>
        {
            var baseUri = new Uri(endpoint.Url, UriKind.Absolute);
            uri = new Uri(baseUri, path);
            return Task.CompletedTask;
        });

        var healthCheckKey = $"{builder.Resource.Name}_{endpointName}_{path}_{statusCode}_check";
        builder.ApplicationBuilder.Services.AddLogging(configure =>
        {
            // The AddUrlGroup health check makes use of http client factory.
            configure.AddFilter($"System.Net.Http.HttpClient.{healthCheckKey}.LogicalHandler", LogLevel.None);
            configure.AddFilter($"System.Net.Http.HttpClient.{healthCheckKey}.ClientHandler", LogLevel.None);
        });

        builder.ApplicationBuilder.Services.AddHealthChecks().AddUrlGroup((UriHealthCheckOptions options) =>
        {
            if (uri is null)
            {
                throw new DistributedApplicationException($"The URI for the health check is not set. Ensure that the resource has been allocated before the health check is executed.");
            }

            options.AddUri(uri, setup => setup.ExpectHttpCode(statusCode ?? 200));
        }, healthCheckKey);

        builder.WithHealthCheck(healthCheckKey);

        return builder;
    }

    /// <summary>
    /// Adds a health check to the resource which is mapped to a specific endpoint.
    /// </summary>
    /// <typeparam name="T">A resource type that implements <see cref="IResourceWithEndpoints" />.</typeparam>
    /// <param name="builder">A resource builder.</param>
    /// <param name="path">The relative path to test.</param>
    /// <param name="statusCode">The result code to interpret as healthy.</param>
    /// <param name="endpointName">The name of the endpoint to derive the base address from.</param>
    /// <returns>A resource builder.</returns>
    /// <remarks>
    /// <para>
    /// This method adds a health check to the health check service which polls the specified endpoint on the resource
    /// on a periodic basis. The base address is dynamically determined based on the endpoint that was selected. By
    /// default the path is set to "/" and the status code is set to 200.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example shows add a HTTPS health check to a backend project
    /// to make sure that the front end does not start until the backend is
    /// reporting a healthy status based on the return code returned from the
    /// "/health" path on the backend server.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// var backend = builder.AddProject&lt;Projects.Backend&gt;("backend")
    ///                      .WithHttpsHealthCheck("/health");
    /// builder.AddProject&lt;Projects.Backend&gt;("backend")
    ///        .WithReference(backend).WaitFor(backend);
    /// </code>
    /// </example>
    public static IResourceBuilder<T> WithHttpsHealthCheck<T>(this IResourceBuilder<T> builder, string? path = null, int? statusCode = null, string? endpointName = null) where T : IResourceWithEndpoints
    {
        endpointName = endpointName ?? "https";
        return builder.WithHttpHealthCheckInternal(
            path: path,
            desiredScheme: "https",
            endpointName: endpointName,
            statusCode: statusCode);
    }

    /// <summary>
    /// Adds a <see cref="ResourceCommandAnnotation"/> to the resource annotations to add a resource command.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="type">The type of command. The type uniquely identifies the command.</param>
    /// <param name="displayName">The display name visible in UI.</param>
    /// <param name="executeCommand">
    /// A callback that is executed when the command is executed. The callback is run inside the .NET Aspire host.
    /// The callback result is used to indicate success or failure in the UI.
    /// </param>
    /// <param name="updateState">
    /// <para>A callback that is used to update the command state. The callback is executed when the command's resource snapshot is updated.</para>
    /// <para>If a callback isn't specified, the command is always enabled.</para>
    /// </param>
    /// <param name="displayDescription">
    /// Optional description of the command, to be shown in the UI.
    /// Could be used as a tooltip. May be localized.
    /// </param>
    /// <param name="parameter">
    /// Optional parameter that configures the command in some way.
    /// Clients must return any value provided by the server when invoking the command.
    /// </param>
    /// <param name="confirmationMessage">
    /// When a confirmation message is specified, the UI will prompt with an OK/Cancel dialog
    /// and the confirmation message before starting the command.
    /// </param>
    /// <param name="iconName">The icon name for the command. The name should be a valid FluentUI icon name. https://aka.ms/fluentui-system-icons</param>
    /// <param name="iconVariant">The icon variant.</param>
    /// <param name="isHighlighted">A flag indicating whether the command is highlighted in the UI.</param>
    /// <returns>The resource builder.</returns>
    /// <remarks>
    /// <para>The <c>WithCommand</c> method is used to add commands to the resource. Commands are displayed in the dashboard
    /// and can be executed by a user using the dashboard UI.</para>
    /// <para>When a command is executed, the <paramref name="executeCommand"/> callback is called and is run inside the .NET Aspire host.</para>
    /// </remarks>
    public static IResourceBuilder<T> WithCommand<T>(
        this IResourceBuilder<T> builder,
        string type,
        string displayName,
        Func<ExecuteCommandContext, Task<ExecuteCommandResult>> executeCommand,
        Func<UpdateCommandStateContext, ResourceCommandState>? updateState = null,
        string? displayDescription = null,
        object? parameter = null,
        string? confirmationMessage = null,
        string? iconName = null,
        IconVariant? iconVariant = null,
        bool isHighlighted = false) where T : IResource
    {
        // Replace existing annotation with the same name.
        var existingAnnotation = builder.Resource.Annotations.OfType<ResourceCommandAnnotation>().SingleOrDefault(a => a.Type == type);
        if (existingAnnotation != null)
        {
            builder.Resource.Annotations.Remove(existingAnnotation);
        }

        return builder.WithAnnotation(new ResourceCommandAnnotation(type, displayName, updateState ?? (c => ResourceCommandState.Enabled), executeCommand, displayDescription, parameter, confirmationMessage, iconName, iconVariant, isHighlighted));
    }
}
