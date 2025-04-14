// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Context passed to callback to configure <see cref="HttpRequestMessage"/> when using
/// <see cref="ResourceBuilderExtensions.WithHttpCommand{TResource}(IResourceBuilder{TResource}, string, string, string?, string?, HttpCommandOptions?)"/>
/// or <see cref="ResourceBuilderExtensions.WithHttpCommand{TResource}(IResourceBuilder{TResource}, string, string, Func{EndpointReference}?, string?, HttpCommandOptions?)"/>.
/// </summary>
public sealed class HttpCommandRequestContext
{
    /// <summary>
    /// The service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The name of the resource the command was configured on.
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// The endpoint the request is targeting.
    /// </summary>
    public required EndpointReference Endpoint { get; init; }

    /// <summary>
    /// The cancellation token.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// The HTTP client to use for the request.
    /// </summary>
    public required HttpClient HttpClient { get; init; }

    /// <summary>
    /// The HTTP request message.
    /// </summary>
    public required HttpRequestMessage Request { get; init; }
}

/// <summary>
/// Context passed to callback to configure <see cref="ExecuteCommandResult"/> when using
/// <see cref="ResourceBuilderExtensions.WithHttpCommand{TResource}(IResourceBuilder{TResource}, string, string, string?, string?, HttpCommandOptions?)"/>
/// or <see cref="ResourceBuilderExtensions.WithHttpCommand{TResource}(IResourceBuilder{TResource}, string, string, Func{EndpointReference}?, string?, HttpCommandOptions?)"/>.
/// </summary>
public sealed class HttpCommandResultContext
{
    /// <summary>
    /// The service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// The name of the resource the command was configured on.
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// The endpoint the request is targeting.
    /// </summary>
    public required EndpointReference Endpoint { get; init; }

    /// <summary>
    /// The cancellation token.
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// The HTTP client that was used for the request.
    /// </summary>
    public required HttpClient HttpClient { get; init; }

    /// <summary>
    /// The HTTP response message.
    /// </summary>
    public required HttpResponseMessage Response { get; init; }
}
