// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// Resource to attach the manifest publishing callback on. The callback records in the manifest the reference between the project and the CloudFormation resource.
/// </summary>
/// <param name="cloudFormationResource"></param>
/// <param name="targetResource"></param>
internal sealed class CloudFormationReferenceResource(ICloudFormationResource cloudFormationResource, IResource targetResource) : Resource($"{targetResource.Name}-{cloudFormationResource.Name}-ref"), IResourceWithEnvironment
{
    internal IResource TargetResource { get; } = targetResource;

    internal ICloudFormationResource CloudFormationResource { get; } = cloudFormationResource;

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cloudformation.reference.v0");
        context.Writer.WriteString("cloudformation", CloudFormationResource.Name);
        context.Writer.WriteString("resource", TargetResource.Name);
    }
}
