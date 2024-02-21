// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Links to a resource that implements <see cref="IResourceWithConnectionString"/> that can be used by the containing resource to acquire a connection string.
/// </summary>
/// <param name="resource">Resource that </param>
public class ConnectionStringRedirectAnnotation(IResourceWithConnectionString resource) : IResourceAnnotation
{
    /// <summary>
    /// Callback to acquire connection string.
    /// </summary>
    public IResourceWithConnectionString Resource => resource;
}
