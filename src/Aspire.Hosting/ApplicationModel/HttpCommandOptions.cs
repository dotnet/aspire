// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Optional configuration for resource HTTP commands added with <see cref="ResourceBuilderExtensions.WithHttpCommand{TResource}(IResourceBuilder{TResource}, string, string, string?, HttpCommandOptions?)"/>."/>
/// </summary>
public class HttpCommandOptions : CommandOptions
{
    internal static new HttpCommandOptions Default { get; } = new HttpCommandOptions();

    /// <summary>
    /// Gets or sets a callback that selects the HTTP endpoint to send the request to when the command is invoked.
    /// </summary>
    public Func<EndpointReference>? EndpointSelector { get; init; }

    /// <summary>
    /// Gets or sets the HTTP method to use when sending the request. Defaults to <c>POST</c>.
    /// </summary>
    public HttpMethod Method { get; init; } = HttpMethod.Post;

    /// <summary>
    /// Gets or sets the name of the HTTP client to use when creating it via <see cref="IHttpClientFactory.CreateClient(string)"/>.
    /// </summary>
    public string? HttpClientName { get; init; }

    /// <summary>
    /// Gets or sets a callback to be invoked to configure the request before it is sent.
    /// </summary>
    public Func<HttpCommandRequestContext, Task>? PrepareRequest { get; init; }

    /// <summary>
    /// Gets or sets a callback to be invoked after the response is received to determine the result of the command invocation.
    /// </summary>
    public Func<HttpCommandResultContext, Task<ExecuteCommandResult>>? GetCommandResult { get; init; }

    /// <summary>
    /// Gets or sets the name of command. The name uniquely identifies the command.
    /// </summary>
    public string? CommandName { get; init; }
}
