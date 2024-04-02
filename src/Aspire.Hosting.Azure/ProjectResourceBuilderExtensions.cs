// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Provides extension methods for building a project resource.
/// </summary>
public static class ProjectResourceBuilderExtensions
{
    /// <summary>
    /// Adds a User Assigned Identity to the project.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="envPrefix">Environment Variable prefix for the Client ID (e.g. {envPrefix}_CLIENT_ID).</param>
    /// <param name="clientId">The identity's Client ID for usage within the app.</param>
    /// <param name="identityId">The identity Resource ID for assignment to the container app.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithUserAssignedIdentity(this IResourceBuilder<ProjectResource> builder, string envPrefix, string clientId, string identityId)
    {
        // Check that we don't already have an annotation with this prefix
        if (builder.Resource.Annotations.OfType<UserAssignedIdentityAnnotation>().Any(m => m.EnvironmentVariablePrefix == envPrefix))
        {
            throw new DistributedApplicationException($"A User Assigned Identity with the env prefix '{envPrefix}' has already been added to the project.");
        }

        builder.WithAnnotation(new UserAssignedIdentityAnnotation(envPrefix, clientId, identityId));
        return builder;
    }

    /// <summary>
    /// Adds a User Assigned Identity to the project using a Bicep output reference.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="envPrefix">The Environment Variable prefix for the Client ID (e.g. {envPrefix}_CLIENT_ID).</param>
    /// <param name="clientIdOutputReference">The bicep output reference for the Client ID.</param>
    /// <param name="identityIdOutputReference">The bicep output reference for the Resource ID.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithUserAssignedIdentity(this IResourceBuilder<ProjectResource> builder, string envPrefix, BicepOutputReference clientIdOutputReference, BicepOutputReference identityIdOutputReference)
    {
        return builder.WithUserAssignedIdentity(envPrefix, clientIdOutputReference.ValueExpression, identityIdOutputReference.ValueExpression);
    }
}
