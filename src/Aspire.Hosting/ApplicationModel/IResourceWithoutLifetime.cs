// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that does not have a lifetime. Reserved for resources that are just holders of data or references to other resources.
/// </summary>
public interface IResourceWithoutLifetime : IResource
{

}
