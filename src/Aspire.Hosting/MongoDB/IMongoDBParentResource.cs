// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a MongoDB resource that requires a connection string.
/// </summary>
public interface IMongoDBParentResource : IResourceWithConnectionString
{
}
