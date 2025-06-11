// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dcp;

/// <summary>
/// Defines a mechanism to select resources from a collection.
/// </summary>
internal interface IResourcesSelector
{
    IResourceCollection Select(IResourceCollection resources);
}
