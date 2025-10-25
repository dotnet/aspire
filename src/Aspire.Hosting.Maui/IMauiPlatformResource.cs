// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Marker interface for MAUI platform-specific resources (Windows, Android, iOS, Mac Catalyst).
/// </summary>
/// <remarks>
/// This interface is used to identify resources that represent a specific platform instance
/// of a MAUI application, allowing for common handling across all MAUI platforms.
/// All MAUI platform resources have a parent <see cref="MauiProjectResource"/>.
/// </remarks>
internal interface IMauiPlatformResource : IResourceWithParent<MauiProjectResource>
{
}
