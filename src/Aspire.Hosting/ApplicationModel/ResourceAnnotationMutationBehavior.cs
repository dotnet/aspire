// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies behavior of the <see cref="IResourceBuilder{T}.WithAnnotation{TAnnotation}(Aspire.Hosting.ApplicationModel.ResourceAnnotationMutationBehavior)" />
/// method when adding an annotation to the <see cref="IResource.Annotations"/> collection of a resource.
/// </summary>
public enum ResourceAnnotationMutationBehavior
{
    /// <summary>
    /// Append the annotation to the collection. Existing annotations will be kept.
    /// </summary>
    Append,
    /// <summary>
    /// Replace the existing annotation. The existing annotation will be removed.
    /// </summary>
    Replace
}
