// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has parameters.
/// </summary>
/// <remarks>
/// This interface can be used to inspect the parameters of a resource and their values.
/// </remarks>
public interface IResourceWithParameters : IResource
{
    /// <summary>
    /// Gets the parameters associated with the resource.
    /// </summary>
    IDictionary<string, object?> Parameters { get; }
}
