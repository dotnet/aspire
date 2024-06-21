// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

internal class ConstructResource(string name, IConstruct construct, IResourceWithConstruct parent) : Resource(name), IConstructResource
{
    public IConstruct Construct { get; } = construct;

    public IResourceWithConstruct Parent { get; } = parent;

    public IStackResource Stack => Parent as IStackResource ?? this.FindParentOfType<IStackResource>();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cdk.construct.v0");
        context.Writer.TryWriteString("construct-name", Name);

        context.Writer.TryWriteString("stack-unique-id", Construct.StackUniqueId());

        context.Writer.WritePropertyName("references");
        context.Writer.WriteStartArray();
        context.Writer.WriteStartObject();
        context.Writer.WriteString("parent-resource", Parent.Name);
        context.Writer.WriteEndObject();
        foreach (var constructResource in Annotations.OfType<ConstructReferenceAnnotation>())
        {
            context.Writer.WriteStartObject();
            context.Writer.WriteString("target-resource", constructResource.TargetResource);
            context.Writer.WriteEndObject();
        }
        context.Writer.WriteEndArray();
    }
}

internal sealed class ConstructResource<T>(string name, T construct, IResourceWithConstruct parent) : ConstructResource(name, construct, parent), IConstructResource<T>
    where T : IConstruct
{
    public new T Construct { get; } = construct;
}
