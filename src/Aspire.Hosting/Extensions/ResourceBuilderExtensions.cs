// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;

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
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, string? value) where T : IResource
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, () => value ?? string.Empty));
    }

    /// <summary>
    /// Adds an environment variable to the resource.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="callback">A callback that allows for deferred execution of a specific enviroment variable. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connetion strings, ports.</param>
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
    /// <param name="callback">A callback that allows for deferred execution for computing many enviroment variables. This runs after resources have been allocated by the orchestrator and allows access to other resources to resolve computed data, e.g. connetion strings, ports.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, Action<EnvironmentCallbackContext> callback) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(callback));
    }

    /// <summary>
    /// Adds an environment variable to the resource with the binding for <paramref name="endpointReference"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="endpointReference">The endpoint from which to extract the url.</param>
    /// <returns>A resource configured with the environment variable callback.</returns>
    public static IResourceBuilder<T> WithEnvironment<T>(this IResourceBuilder<T> builder, string name, EndpointReference endpointReference) where T : IResourceWithEnvironment
    {
        return builder.WithAnnotation(new EnvironmentCallbackAnnotation(name, () =>
        {
            var replaceLocalhostWithContainerHost = builder.Resource is ContainerResource;

            return replaceLocalhostWithContainerHost
            ? HostNameResolver.ReplaceLocalhostWithContainerHost(endpointReference.UriString, builder.ApplicationBuilder.Configuration)
            : endpointReference.UriString;
        }));
    }

    /// <summary>
    /// Registers a callback which is invoked when manifest is generated for the app model.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="callback">Callback method which takes a <see cref="ManifestPublishingContext"/> which can be used to inject JSON into the manifest.</param>
    /// <returns></returns>
    public static IResourceBuilder<T> WithManifestPublishingCallback<T>(this IResourceBuilder<T> builder, Action<ManifestPublishingContext> callback) where T : IResource
    {
        return builder.WithAnnotation(new ManifestPublishingCallbackAnnotation(callback));
    }

    private static bool ContainsAmbiguousEndpoints(IEnumerable<AllocatedEndpointAnnotation> endpoints)
    {
        // An ambiguous endpoint is where any scheme (
        return endpoints.GroupBy(e => e.UriScheme).Any(g => g.Count() > 1);
    }

    private static Action<EnvironmentCallbackContext> CreateEndpointReferenceEnvironmentPopulationCallback<T>(IResourceBuilder<T> builder, EndpointReferenceAnnotation endpointReferencesAnnotation)
        where T : IResourceWithEnvironment
    {
        return (context) =>
        {
            var name = endpointReferencesAnnotation.Resource.Name;

            var allocatedEndPoints = endpointReferencesAnnotation.Resource.Annotations
                .OfType<AllocatedEndpointAnnotation>()
                .Where(a => endpointReferencesAnnotation.UseAllEndpoints || endpointReferencesAnnotation.EndpointNames.Contains(a.Name));

            var containsAmbiguousEndpoints = ContainsAmbiguousEndpoints(allocatedEndPoints);

            var replaceLocalhostWithContainerHost = builder.Resource is ContainerResource;
            var configuration = builder.ApplicationBuilder.Configuration;

            var i = 0;
            foreach (var allocatedEndPoint in allocatedEndPoints)
            {
                var bindingNameQualifiedUriStringKey = $"services__{name}__{i++}";
                context.EnvironmentVariables[bindingNameQualifiedUriStringKey] = replaceLocalhostWithContainerHost
                ? HostNameResolver.ReplaceLocalhostWithContainerHost(allocatedEndPoint.BindingNameQualifiedUriString, configuration)
                : allocatedEndPoint.BindingNameQualifiedUriString;

                if (!containsAmbiguousEndpoints)
                {
                    var uriStringKey = $"services__{name}__{i++}";
                    context.EnvironmentVariables[uriStringKey] = replaceLocalhostWithContainerHost
                    ? HostNameResolver.ReplaceLocalhostWithContainerHost(allocatedEndPoint.UriString, configuration)
                    : allocatedEndPoint.UriString;
                }
            }
        };
    }

    /// <summary>
    /// Injects a connection string as an environment variable from the source resource into the destination resource, using the source resource's name as the connection string name (if not overridden).
    /// The format of the environment variable will be "ConnectionStrings__{sourceResourceName}={connectionString}."
    /// <para>
    /// Each resource defines the format of the connection string value. The
    /// underlying connection string value can be retrieved using <see cref="IResourceWithConnectionString.GetConnectionString"/>.
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
    /// <exception cref="DistributedApplicationException">Throws an exception if the connection string resolves to null. It can be null if the resource has no connection string, and if the configurtion has no connection string for the source resource.</exception>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithConnectionString> source, string? connectionName = null, bool optional = false)
        where TDestination : IResourceWithEnvironment
    {
        var resource = source.Resource;
        connectionName ??= resource.Name;

        return builder.WithEnvironment(context =>
        {
            var connectionStringName = $"{ConnectionStringEnvironmentName}{connectionName}";

            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[connectionStringName] = $"{{{resource.Name}.connectionString}}";
                return;
            }

            var connectionString = resource.GetConnectionString() ??
                builder.ApplicationBuilder.Configuration.GetConnectionString(resource.Name);

            if (string.IsNullOrEmpty(connectionString))
            {
                if (optional)
                {
                    // This is an optional connection string, so we can just return.
                    return;
                }

                throw new DistributedApplicationException($"A connection string for '{resource.Name}' could not be retrieved.");
            }

            if (builder.Resource is ContainerResource)
            {
                connectionString = HostNameResolver.ReplaceLocalhostWithContainerHost(connectionString, builder.ApplicationBuilder.Configuration);
            }

            context.EnvironmentVariables[connectionStringName] = connectionString;
        });
    }

    /// <summary>
    /// Injects service discovery information as environment variables from the project resource into the destination resource, using the source resource's name as the service name.
    /// Each endpoint defined on the project resource will be injected using the format "services__{sourceResourceName}__{bindingIndex}={bindingNameQualifiedUriString}."
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="source">The resource from which to extract service discovery information.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, IResourceBuilder<IResourceWithServiceDiscovery> source)
        where TDestination : IResourceWithEnvironment
    {
        ApplyEndpoints(builder, source.Resource);
        return builder;
    }

    /// <summary>
    /// Injects service discovery information as environment variables from the uri into the destination resource, using the name as the service name.
    /// The uri will be injected using the format "services__{name}={uri}."
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="name">The name of the service.</param>
    /// <param name="uri">The uri of the service.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
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

        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables[$"services__{name}"] = uri.ToString();
        });
    }

    /// <summary>
    /// Injects a connection string as an environment variable. The format of the environment variable will be "ConnectionStrings__{name}={value}." If the
    /// connection string is not specified, the configuration system will be queried for a connection string using the connection string name.
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="builder">The resource where connection string will be injected.</param>
    /// <param name="connectionString">A connection string</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, ConnectionString connectionString)
        where TDestination : IResourceWithEnvironment
    {
        return builder.WithEnvironment(context =>
        {
            var connectionStringValue = connectionString.Value ??
                builder.ApplicationBuilder.Configuration.GetConnectionString(connectionString.Name);

            if (string.IsNullOrEmpty(connectionStringValue))
            {
                throw new DistributedApplicationException($"A connection string for '{connectionString.Name}' could not be retrieved.");
            }

            if (builder.Resource is ContainerResource)
            {
                connectionStringValue = HostNameResolver.ReplaceLocalhostWithContainerHost(connectionStringValue, builder.ApplicationBuilder.Configuration);
            }

            context.EnvironmentVariables[$"{ConnectionStringEnvironmentName}{connectionString.Name}"] = connectionStringValue;
        });
    }

    /// <summary>
    /// Injects service discovery information from the specified endpoint into the project resource using the source resource's name as the service name.
    /// Each endpoint will be injected using the format "services__{sourceResourceName}__{bindingIndex}={bindingNameQualifiedUriString}."
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="endpointReference">The endpoint from which to extract the url.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{TDestination}"/>.</returns>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(this IResourceBuilder<TDestination> builder, EndpointReference endpointReference)
        where TDestination : IResourceWithEnvironment
    {
        ApplyEndpoints(builder, endpointReference.Owner, endpointReference.BindingName);
        return builder;
    }

    private static void ApplyEndpoints<T>(this IResourceBuilder<T> builder, IResourceWithBindings resourceWithBindings, string? bindingName = null)
        where T : IResourceWithEnvironment
    {
        // When adding a endpoint we get to see whether there is a EndpointReferenceAnnotation
        // on the resource, if there is then it means we have already been here before and we can just
        // skip this and note the endpoint that we want to apply to the environment in the future
        // in a single pass. There is one EndpointReferenceAnnotation per endpoint source.
        var endpointReferenceAnnotation = builder.Resource.Annotations
            .OfType<EndpointReferenceAnnotation>()
            .Where(sra => sra.Resource == resourceWithBindings)
            .SingleOrDefault();

        if (endpointReferenceAnnotation == null)
        {
            endpointReferenceAnnotation = new EndpointReferenceAnnotation(resourceWithBindings);
            builder.WithAnnotation(endpointReferenceAnnotation);

            var callback = CreateEndpointReferenceEnvironmentPopulationCallback(builder, endpointReferenceAnnotation);
            builder.WithEnvironment(callback);
        }

        // If no specific binding name is specified, go and add all the bindings.
        if (bindingName == null)
        {
            endpointReferenceAnnotation.UseAllEndpoints = true;
        }
        else
        {
            endpointReferenceAnnotation.EndpointNames.Add(bindingName);
        }
    }

    /// <summary>
    /// Exposes an endpoint on a resource. This binding reference can be retrieved using <see cref="GetEndpoint{T}(IResourceBuilder{T}, string)"/>.
    /// The binding name will be the scheme name if not specified.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="hostPort">The host port.</param>
    /// <param name="scheme">The scheme e.g. (http/https)</param>
    /// <param name="name">The name of the binding.</param>
    /// <param name="env">The name of the environment variable to inject.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <exception cref="DistributedApplicationException">Throws an exception if the a binding with the same name already exists on the specified resource.</exception>
    public static IResourceBuilder<T> WithEndpoint<T>(this IResourceBuilder<T> builder, int? hostPort = null, string? scheme = null, string? name = null, string? env = null) where T : IResource
    {
        if (builder.Resource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.Name == name))
        {
            throw new DistributedApplicationException($"Endpoint '{name}' already exists");
        }

        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: scheme, name: name, port: hostPort, env: env);
        return builder.WithAnnotation(annotation);
    }

    /// <summary>
    /// Gets an <see cref="EndpointReference"/> by name from the resource. These endpoints are declared either using <see cref="WithEndpoint{T}(IResourceBuilder{T}, int?, string?, string?, string?)"/> or by launch settings (for project resources).
    /// The <see cref="EndpointReference"/> can be used to resolve the address of the endpoint in <see cref="WithEnvironment{T}(IResourceBuilder{T}, Action{EnvironmentCallbackContext})"/>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The the resource builder.</param>
    /// <param name="name">The name of the endpoint.</param>
    /// <returns>An <see cref="EndpointReference"/> that can be used to resolve the address of the endpoint after resource allocation has occurred.</returns>
    public static EndpointReference GetEndpoint<T>(this IResourceBuilder<T> builder, string name) where T : IResourceWithBindings
    {
        return builder.Resource.GetEndpoint(name);
    }

    /// <summary>
    /// Configures a resource to mark all endpoints' transport as HTTP/2. This is useful for HTTP/2 services that need prior knowledge.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> AsHttp2Service<T>(this IResourceBuilder<T> builder) where T : IResourceWithBindings
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
        foreach (var annotation in builder.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>())
        {
            builder.Resource.Annotations.Remove(annotation);
        }

        return builder.WithAnnotation(ManifestPublishingCallbackAnnotation.Ignore);
    }
}
