// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui.Annotations;

/// <summary>
/// Annotation to mark a resource as running on an unsupported platform.
/// This prevents lifecycle commands and sets the state to "Unsupported".
/// </summary>
/// <param name="reason">The reason why the platform is unsupported.</param>
internal sealed class UnsupportedPlatformAnnotation(string reason) : IResourceAnnotation
{
    /// <summary>
    /// Gets the reason why the platform is unsupported.
    /// </summary>
    public string Reason { get; } = reason;
}
