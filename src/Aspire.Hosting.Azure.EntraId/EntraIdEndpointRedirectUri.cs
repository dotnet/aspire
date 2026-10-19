// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE002 // IComputeEnvironmentResource.GetHostAddressExpression is experimental.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A redirect URI built from a resource endpoint and a caller-supplied callback path.
/// </summary>
internal sealed class EntraIdEndpointRedirectUri(EndpointReference endpoint, string path, bool isPublishMode)
{
    public EndpointReference Endpoint { get; } = endpoint;

    public string Path { get; } = path;

    public ReferenceExpression CreateExpression()
    {
        // The callback path is configured by the consuming application (for example, Microsoft.Identity.Web's
        // CallbackPath), so it can't be discovered from the endpoint and is supplied by the AppHost.
        if (!isPublishMode)
        {
            // Run mode: the allocated URL (for example https://localhost:7123) is available once endpoints are
            // allocated, which happens before any workload starts, so provisioning can wait on it without a deadlock.
            return ReferenceExpression.Create($"{Endpoint.Property(EndpointProperty.Url)}{Path}");
        }

        // Publish/deploy: ask the compute environment that hosts the resource for its public host name, the same way
        // Azure Front Door resolves origin host names. Deployed endpoints are always served over HTTPS.
        if (!ComputeEnvironmentEndpointResolver.TryGetEffectiveComputeEnvironment(Endpoint.Resource, out var computeEnvironment))
        {
            throw new InvalidOperationException(
                $"Resource '{Endpoint.Resource.Name}' does not have a compute environment. " +
                "Ensure a compute environment (for example, Azure Container Apps or Azure App Service) is configured in the application model " +
                "or register the redirect URI with an explicit URL.");
        }

        var host = computeEnvironment.GetHostAddressExpression(Endpoint);
        return ReferenceExpression.Create($"https://{host}{Path}");
    }
}
