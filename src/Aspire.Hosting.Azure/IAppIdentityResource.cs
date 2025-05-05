// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// An Azure resource that represents an application's managed identity.
/// </summary>
public interface IAppIdentityResource
{
    /// <summary>
    /// Gets the unique identifier for the managed identity resource.
    /// </summary>
    public BicepOutputReference Id { get; }

    /// <summary>
    /// Gets the unique identifier for the application client associated with the managed identity.
    /// </summary>
    public BicepOutputReference ClientId { get; }

    /// <summary>
    /// Gets the unique identifier for the security principal associated with the managed identity.
    /// </summary>
    public BicepOutputReference PrincipalId { get; }

    /// <summary>
    /// Gets the name of the security principal associated with the managed identity.
    /// </summary>
    public BicepOutputReference PrincipalName { get; }
}
