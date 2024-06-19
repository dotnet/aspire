// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <inheritdoc cref="Aspire.Hosting.AWS.CloudFormation.ICloudFormationTemplateResource" />
internal sealed class CloudFormationTemplateResource(string name, string templatePath) : CloudFormationResource(name), ICloudFormationTemplateResource, ICloudFormationTemplateProvider
{
    public IDictionary<string, string> CloudFormationParameters { get; } = new Dictionary<string, string>();

    /// <inheritdoc/>
    public string TemplatePath { get; } = templatePath;

    /// <inheritdoc cref="ICloudFormationTemplateResource.RoleArn" />
    public string? RoleArn { get; set; }

    /// <inheritdoc cref="ICloudFormationTemplateResource.StackPollingInterval" />
    public int StackPollingInterval { get; set; } = 3;

    /// <inheritdoc cref="ICloudFormationTemplateResource.DisableDiffCheck" />
    public bool DisableDiffCheck { get; set; }

    /// <inheritdoc cref="ICloudFormationTemplateResource.DisabledCapabilities" />
    public IList<string> DisabledCapabilities { get; } = [];

    /// <inheritdoc/>
    public ICloudFormationTemplateResource AddParameter(string parameterName, string parameterValue)
    {
        CloudFormationParameters[parameterName] = parameterValue;
        return this;
    }

    internal override void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cloudformation.template.v0");
        context.Writer.TryWriteString("stack-name", Name);
        context.Writer.TryWriteString("template-path", context.GetManifestRelativePath(TemplatePath));

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

    string ICloudFormationTemplateProvider.StackName => Name;

    Task<string> ICloudFormationTemplateProvider.GetCloudFormationTemplate(CancellationToken cancellationToken)
        => File.ReadAllTextAsync(TemplatePath, cancellationToken);
}
