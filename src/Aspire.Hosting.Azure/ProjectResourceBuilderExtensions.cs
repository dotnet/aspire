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
    /// <param name="identityId">The ID of the Managed Identity.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithUserAssignedIdentity(this IResourceBuilder<ProjectResource> builder, string identityId)
    {
        builder.WithAnnotation(new UserAssignedIdentityAnnotation(identityId));
        return builder;
    }

    /// <summary>
    /// Adds a User Assigned Identity to the project using a Bicep output reference.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="bicepOutputReference">The reference to the bicep output.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<ProjectResource> WithUserAssignedIdentity(this IResourceBuilder<ProjectResource> builder, BicepOutputReference bicepOutputReference)
    {
        return builder.WithAnnotation(new UserAssignedIdentityAnnotation(bicepOutputReference.ValueExpression));
    }
}
