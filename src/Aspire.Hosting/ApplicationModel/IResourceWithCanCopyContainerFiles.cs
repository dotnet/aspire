// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that can consume and copy files from resources that implement <see cref="IResourceWithContainerFiles"/>.
/// </summary>
/// <remarks>
/// Resources that implement this interface can use the <c>PublishWithContainerFiles</c> extension method
/// to copy files from other container resources during the build/publish process. This is typically used
/// to include static assets, build artifacts, or other files from one resource into another resource's
/// container image during publishing.
/// </remarks>
public interface IResourceWithCanCopyContainerFiles : IResource
{
}
