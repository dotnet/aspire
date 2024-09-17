// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc cref="Aspire.Hosting.AWS.CloudFormation.ICloudFormationStackResource" />
internal sealed class CloudFormationStackResource(string name, string stackName)
    : CloudFormationResource(name, stackName), ICloudFormationStackResource
{
    internal override void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cloudformation.stack.v0");
        context.Writer.TryWriteString("stack-name", StackName);

        context.Writer.WritePropertyName("references");
        context.Writer.WriteStartArray();
        foreach (var cloudFormationResource in Annotations.OfType<CloudFormationReferenceAnnotation>())
        {
            context.Writer.WriteStartObject();
            context.Writer.WriteString("target-resource", cloudFormationResource.TargetResource);
            context.Writer.WriteEndObject();
        }

        context.Writer.WriteEndArray();
    }
}
