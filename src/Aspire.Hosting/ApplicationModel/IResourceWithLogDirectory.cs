// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has a log directory.
/// </summary>
public interface IResourceWithLogDirectory
{
    /// <value>The path to the log directory for the resource (e.g. inside of a container).</value>
    public static abstract string LogDirectory { get; }
}
