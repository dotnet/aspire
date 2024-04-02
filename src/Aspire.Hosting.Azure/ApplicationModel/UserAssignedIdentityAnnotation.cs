// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.ApplicationModel;

/// <summary>
/// Represents a user assigned identity that should be assigned to a project.
/// </summary>
public class UserAssignedIdentityAnnotation : CustomManifestOutputAnnotation
{
    /// <summary>
    /// Initializes a new instance of <see cref="UserAssignedIdentityAnnotation"/>.
    /// </summary>
    /// <param name="identityId">The identity ID to be assigned to the </param>
    public UserAssignedIdentityAnnotation(string identityId) : base("userAssignedIdentities", identityId)
    {
    }
}
