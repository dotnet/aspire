// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has a connection string associated with it.
/// </summary>
public interface IResourceWithConnectionString : IResource
{
    /// <summary>
    /// Gets the connection string associated with the resource.
    /// </summary>
    /// <returns>The connection string associated with the resource, when one is available.</returns>
    public string? GetConnectionString();
}
