// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Marker annotation to indicate that a resource is a test resource.
/// Test resources are expected to run to completion and then exit.
/// </summary>
public sealed class TestResourceAnnotation : IResourceAnnotation
{
}
