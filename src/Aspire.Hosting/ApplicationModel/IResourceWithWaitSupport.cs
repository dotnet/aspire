// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that can wait for other resources to be running, health, and/or completed.
/// </summary>
public interface IResourceWithWaitSupport : IResource
{
}
