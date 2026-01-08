// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure compute environment resource.
/// </summary>
public interface IAzureComputeEnvironmentResource : IComputeEnvironmentResource
{
    /// <summary>
    /// Gets the user-assigned managed identity associated with this compute environment.
    /// </summary>
    AzureUserAssignedIdentityResource? UserAssignedIdentity { get; }
}