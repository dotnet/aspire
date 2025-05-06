// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Provides extension methods for working with Azure user‑assigned identities.
/// </summary>
public static class AzureUserAssignedIdentityExtensions
{
    /// <summary>
    /// Adds an Azure user‑assigned identity resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or empty.</exception>
    /// <remarks>
    /// This method adds an Azure user‑assigned identity resource to the application model. It configures the
    /// infrastructure for the resource and returns a builder for the resource.
    /// The resource is added to the infrastructure only if the application is not in run mode.
    /// </remarks>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureUserAssignedIdentityResource}"/> builder.</returns>
    public static IResourceBuilder<AzureUserAssignedIdentityResource> AddAzureUserAssignedIdentity(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        var resource = new AzureUserAssignedIdentityResource(name);
        // Don't add the resource to the infrastructure if we're in run mode.
        if (builder.ExecutionContext.IsRunMode)
        {
            return builder.CreateResourceBuilder(resource);
        }

        return builder.AddResource(resource);
    }
}
