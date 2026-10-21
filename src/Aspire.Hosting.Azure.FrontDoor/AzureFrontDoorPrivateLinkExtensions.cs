// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for reaching a compute environment, such as Azure Container Apps, through Azure Front Door Private Link.
/// </summary>
public static class AzureFrontDoorPrivateLinkExtensions
{
    /// <summary>
    /// Connects an Azure Front Door to a compute environment, such as an Azure Container Apps environment, over Private
    /// Link and disables public network access to the environment, so that the apps in the environment are reachable only
    /// through the Front Door.
    /// </summary>
    /// <typeparam name="T">The type of the compute environment, which implements <see cref="IAzureFrontDoorPrivateLinkTarget"/>.</typeparam>
    /// <param name="environment">The compute environment to connect to.</param>
    /// <param name="frontDoor">
    /// The Azure Front Door, created with <see cref="AzureFrontDoorExtensions.AddAzureFrontDoor"/>, whose origins in
    /// <paramref name="environment"/> use Private Link.
    /// </param>
    /// <param name="privateLinkLocation">
    /// The Azure region of the private endpoint that Front Door creates, such as <c>westus2</c> or <c>West US 2</c>.
    /// The default, <see langword="null"/>, uses the region of <paramref name="environment"/>. Front Door Private Link
    /// is only available in some regions, so set this when the environment's region isn't one of them.
    /// </param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/> for <paramref name="environment"/>, for chaining.</returns>
    /// <ats-returns>The resource builder.</ats-returns>
    /// <remarks>
    /// <para>
    /// Add the apps to the Front Door with <see cref="AzureFrontDoorExtensions.WithOrigin"/>, before or after this call.
    /// When publishing, every origin of <paramref name="frontDoor"/> that runs in <paramref name="environment"/> becomes a
    /// Private Link origin: the Front Door profile uses the Premium SKU, the origin connects to the environment through a
    /// Front Door managed private endpoint, and Front Door forwards requests and probes the origin over HTTPS, because the
    /// environment's ingress terminates TLS. The apps must expose external HTTP endpoints, for example with
    /// <c>WithExternalHttpEndpoints</c>. Origins in other compute environments are left as they are.
    /// </para>
    /// <para>
    /// Like a private endpoint to any other Azure resource, the Private Link disables public network access to the
    /// environment. Apps in the environment are no longer reachable from the Internet except through Front Door, and
    /// references between apps in the environment use the environment's internal host names. Callers outside the
    /// environment must reach the apps through the Front Door, or through a route of their own.
    /// </para>
    /// <para>
    /// An Azure Container Apps environment can only have its public network access disabled when Aspire provisions it, so it
    /// can't be marked as existing, and it must be a workload profiles environment, not an
    /// <see href="https://learn.microsoft.com/azure/container-apps/express-overview">Azure Container Apps Express</see>
    /// environment.
    /// </para>
    /// <para>
    /// Front Door creates a connection request in the Pending state, and no traffic reaches the apps until the owner of
    /// the environment approves it. Approve it in the Azure portal, under the environment's networking settings, or with
    /// the Azure CLI by finding the request with <c>az network private-endpoint-connection list</c> and approving it with
    /// <c>az network private-endpoint-connection approve</c>. Front Door can create more than one request; approve each
    /// one that has the message of this connection. Traffic starts flowing a few minutes after approval.
    /// </para>
    /// <para>
    /// Private Link has no run-mode counterpart, so this method does nothing when the AppHost runs locally.
    /// </para>
    /// </remarks>
    /// <ats-remarks>
    /// <para>
    /// Add the apps to the Front Door with <c>withOrigin</c>, before or after this call. Every origin of the Front Door that
    /// runs in the environment becomes a Private Link origin: the Front Door profile uses the Premium SKU, and Front Door
    /// forwards requests and probes the origin over HTTPS. The apps must expose external HTTP endpoints. Origins in other
    /// compute environments are left as they are.
    /// </para>
    /// <para>
    /// Public network access to the environment is disabled, so the apps are reachable only through the Front Door, and
    /// references between apps in the environment use the environment's internal host names. The environment must be a
    /// workload profiles environment that Aspire provisions.
    /// </para>
    /// <para>
    /// Front Door creates a connection request in the Pending state, and no traffic reaches the apps until the owner of
    /// the environment approves it in the Azure portal or with <c>az network private-endpoint-connection approve</c>.
    /// </para>
    /// </ats-remarks>
    /// <exception cref="ArgumentNullException"><paramref name="environment"/> or <paramref name="frontDoor"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="privateLinkLocation"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// The environment is already connected to <paramref name="frontDoor"/> with a different
    /// <paramref name="privateLinkLocation"/>. Publishing also fails when the environment is marked as existing or is
    /// an Express environment, when <paramref name="frontDoor"/> has no origin in the environment, and when another Front
    /// Door has an origin in the environment without being connected to it.
    /// </exception>
    /// <example>
    /// <code lang="csharp">
    /// var environment = builder.AddAzureContainerAppEnvironment("env");
    /// var frontDoor = builder.AddAzureFrontDoor("frontdoor");
    /// environment.WithAzureFrontDoorPrivateLink(frontDoor);
    ///
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///     .WithExternalHttpEndpoints();
    /// var web = builder.AddProject&lt;Projects.Web&gt;("web")
    ///     .WithExternalHttpEndpoints()
    ///     .WithReference(api);
    /// frontDoor.WithOrigin(api).WithOrigin(web);
    /// </code>
    /// </example>
    /// <seealso href="https://learn.microsoft.com/azure/container-apps/how-to-integrate-with-azure-front-door">Access a container app using Azure Front Door with Private Link</seealso>
    [AspireExport("withFrontDoorAzureFrontDoorPrivateLink", MethodName = "withAzureFrontDoorPrivateLink")]
    [Experimental("ASPIREAZURE003", UrlFormat = "https://aka.ms/aspire/diagnostics#{0}")]
    public static IResourceBuilder<T> WithAzureFrontDoorPrivateLink<T>(
        this IResourceBuilder<T> environment,
        IResourceBuilder<AzureFrontDoorResource> frontDoor,
        string? privateLinkLocation = null)
        where T : IAzureFrontDoorPrivateLinkTarget
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(frontDoor);
        if (privateLinkLocation is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(privateLinkLocation);
        }

        if (!environment.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return environment;
        }

        // Accept display names such as 'West US 2' as well as programmatic names such as 'westus2'.
        var location = privateLinkLocation?.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

        var existing = frontDoor.Resource.Annotations
            .OfType<AzureFrontDoorPrivateLinkAnnotation>()
            .FirstOrDefault(annotation => ReferenceEquals(annotation.Environment, environment.Resource));
        if (existing is not null)
        {
            if (!string.Equals(existing.PrivateLinkLocation, location, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Compute environment '{environment.Resource.Name}' is already connected to Azure Front Door " +
                    $"'{frontDoor.Resource.Name}' with a different Private Link location. Call {nameof(WithAzureFrontDoorPrivateLink)} " +
                    "once for each Front Door, or pass the same location.");
            }

            return environment;
        }

        frontDoor.WithAnnotation(new AzureFrontDoorPrivateLinkAnnotation(frontDoor.Resource, environment.Resource, location));

        // A private link target denies public network access, as it does for a private endpoint to any other Azure
        // resource. The Front Door is the resource that creates the managed private endpoint.
        environment.WithAnnotation(new PrivateEndpointTargetAnnotation(frontDoor.Resource));

        return environment;
    }
}
