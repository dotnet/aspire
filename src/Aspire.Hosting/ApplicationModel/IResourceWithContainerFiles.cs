// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents a resource that contains files that can be copied to other resources.
/// </summary>
/// <remarks>
/// Resources that implement this interface produce container images that include files
/// that can be copied into other resources. For example using Docker's COPY --from feature.
/// </remarks>
public interface IResourceWithContainerFiles : IResource
{
}
