// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation that indicates a resource is the target of a private endpoint.
/// When this annotation is present, the resource should be configured to deny public network access.
/// </summary>
public sealed class PrivateEndpointTargetAnnotation : IResourceAnnotation
{
}
