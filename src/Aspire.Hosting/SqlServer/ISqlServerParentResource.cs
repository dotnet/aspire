// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a SQL Server resource that requires a connection string.
/// </summary>
public interface ISqlServerParentResource : IResourceWithConnectionString
{
}
