// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS.CDK;

internal static class ResourceExtensions
{
    /// <summary>
    /// Resolves a <see cref="CloudFormationStackArtifact"/> from a resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="stackArtifact"></param>
    /// <returns>False when no <see cref="StackArtifactResourceAnnotation"/> is not found as annotation of the resource.</returns>
    public static bool TryGetStackArtifact(this IStackResource resource, [NotNullWhen(true)] out CloudFormationStackArtifact? stackArtifact)
    {
        stackArtifact = default;
        if (!resource.TryGetAnnotationsOfType<StackArtifactResourceAnnotation>(out var annotations))
        {
            return false;
        }
        stackArtifact = annotations.First().StackArtifact;
        return true;
    }
}
