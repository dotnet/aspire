// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation for an application's identity resource.
/// </summary>
/// <remarks>
/// The identity resource represents the Azure managed identity associated with the application.
/// </remarks>
/// <param name="identityResource">The identity resource associated with the application.</param>
public class AppIdentityAnnotation(IAppIdentityResource identityResource) : IResourceAnnotation
{
    /// <summary>
    /// Gets the identity resource associated with an application.
    /// </summary>
    public IAppIdentityResource IdentityResource { get; } = identityResource;
}
