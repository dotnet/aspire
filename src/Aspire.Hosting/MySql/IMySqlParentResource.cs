// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a MySQL parent resource (container or server) that produces a connection string.
/// </summary>
public interface IMySqlParentResource : IResourceWithConnectionString, IResourceWithEnvironment
{
}
