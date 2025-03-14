// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that supports Entity Framework migrations.
/// </summary>
public interface IResourceSupportsEntityFrameworkMigrations : IResource, IResourceWithParent, IResourceWithConnectionString
{
}