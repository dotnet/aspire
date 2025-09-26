// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Annotation applied to platform resources that were auto-detected from the MAUI project's target frameworks.
/// Used only for user-facing warnings encouraging explicit platform configuration.
/// </summary>
internal sealed class MauiAutoDetectedPlatformAnnotation : IResourceAnnotation { }

/// <summary>
/// Annotation applied to platform resources that are not supported on the current host OS.
/// Carries a human-readable reason used in warnings and startup validation.
/// </summary>
/// <param name="reason">Explanation why the platform cannot run on this host.</param>
internal sealed class MauiUnsupportedPlatformAnnotation(string reason) : IResourceAnnotation
{
    public string Reason { get; } = reason;
}
