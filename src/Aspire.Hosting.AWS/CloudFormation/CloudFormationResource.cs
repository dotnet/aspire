// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc/>
internal abstract class CloudFormationResource(string name) : Resource(name), ICloudFormationResource
{
    /// <inheritdoc/>
    public IAWSSDKConfig? AWSSDKConfig { get; set; }

    /// <inheritdoc/>
    public IAmazonCloudFormation? CloudFormationClient { get; set; }

    /// <inheritdoc/>
    public List<Output>? Outputs { get; set; }

    /// <inheritdoc/>
    public TaskCompletionSource? ProvisioningTaskCompletionSource { get; set; }

    protected virtual string GetStackName() => Name;

    internal virtual void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cloudformation.stack.v0");
        context.Writer.TryWriteString("stack-name", GetStackName());
        var templatePathAnnotation = Annotations.OfType<CloudFormationTemplatePathAnnotation>().FirstOrDefault();
        if (templatePathAnnotation != null)
        {
            context.Writer.TryWriteString("template-path", context.GetManifestRelativePath(templatePathAnnotation.TemplatePath));
        }

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
