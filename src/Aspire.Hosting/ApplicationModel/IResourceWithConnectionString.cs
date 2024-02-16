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

    /// <summary>
    /// Describes the connection string format string used for this resource in the manifest.
    /// </summary>
    public string? ConnectionStringExpression => null;

    /// <summary>
    /// The expression used in the manifest to reference the connection string.
    /// </summary>
    public string ConnectionStringReferenceExpression => $"{{{Name}.connectionString}}";

    /// <summary>
    /// The environment variable name to use for the connection string.
    /// </summary>
    public string? ConnectionStringEnvironmentVariable => null;
}
