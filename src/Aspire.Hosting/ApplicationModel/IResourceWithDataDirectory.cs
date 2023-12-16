// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that stores data in a specific directory.
/// </summary>
public interface IResourceWithDataDirectory
{
    /// <value>The path to the data directory for the resource (e.g. inside of a container).</value>
    public static abstract string DataDirectory { get; }
}
