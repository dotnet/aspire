// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds a command to the resource that sends an HTTP request to the specified path.
    /// </summary>
    public static IResourceBuilder<TResource> WithHttpsCommand<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        HttpMethod? method = default,
        string? endpointName = default,
        string? iconName = default)
        where TResource : IResourceWithEndpoints
        => WithHttpCommandImpl(builder, path, displayName, endpointName ?? "https", method, "https", iconName);

    /// <summary>
    /// Adds a command to the resource that sends an HTTP request to the specified path.
    /// </summary>
    public static IResourceBuilder<TResource> WithHttpCommand<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        HttpMethod? method = default,
        string? endpointName = default,
        string? iconName = default)
        where TResource : IResourceWithEndpoints
        => WithHttpCommandImpl(builder, path, displayName, endpointName ?? "http", method, "http", iconName);

    private static IResourceBuilder<TResource> WithHttpCommandImpl<TResource>(this IResourceBuilder<TResource> builder,
        string path,
        string displayName,
        string endpointName,
        HttpMethod? method,
        string expectedScheme,
        string? iconName = default)
        where TResource : IResourceWithEndpoints
    {
        method ??= HttpMethod.Post;

        var endpoints = builder.Resource.GetEndpoints();
        var endpoint = endpoints.FirstOrDefault(e => string.Equals(e.EndpointName, endpointName, StringComparison.OrdinalIgnoreCase))
            ?? throw new DistributedApplicationException($"Could not create HTTP command for resource '{builder.Resource.Name}' as no endpoint named '{endpointName}' was found.");

        var commandType = $"http-{method.Method.ToLowerInvariant()}-{path.ToLowerInvariant()}-request";

        builder.WithCommand(commandType, displayName, async context =>
        {
            if (!endpoint.IsAllocated)
            {
                return new ExecuteCommandResult { Success = false, ErrorMessage = "Endpoints are not yet allocated." };
            }

            if (!string.Equals(endpoint.Scheme, expectedScheme, StringComparison.OrdinalIgnoreCase))
            {
                return new ExecuteCommandResult { Success = false, ErrorMessage = $"The endpoint named '{endpointName}' on resource '{builder.Resource.Name}' does not have the expected scheme of '{expectedScheme}'." };
            }

            var uri = new UriBuilder(endpoint.Url) { Path = path }.Uri;
            var httpClient = context.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            var request = new HttpRequestMessage(method, uri);
            try
            {
                var response = await httpClient.SendAsync(request, context.CancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                return new ExecuteCommandResult { Success = false, ErrorMessage = ex.Message };
            }
            return new ExecuteCommandResult { Success = true };
        },
        iconName: iconName,
        iconVariant: IconVariant.Regular);

        return builder;
    }
}
