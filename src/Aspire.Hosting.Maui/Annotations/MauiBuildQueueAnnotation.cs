// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui.Annotations;

/// <summary>
/// Annotation added to <see cref="MauiProjectResource"/> to serialize builds across
/// platform resources that share the same project.
/// </summary>
internal sealed class MauiBuildQueueAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets the semaphore used to serialize builds for this project.
    /// </summary>
    public SemaphoreSlim BuildSemaphore { get; } = new(1, 1);
}
