// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Constructs;

namespace Aspire.Hosting.AWS.CDK;

/// <inheritdoc cref="Aspire.Hosting.AWS.CDK.IConstructResource" />
internal class ConstructResource(string name, IConstruct construct, IResourceWithConstruct parent) : Resource(name), IConstructResource
{
    /// <inheritdoc/>
    public IConstruct Construct { get; } = construct;

    /// <inheritdoc/>
    public IResourceWithConstruct Parent { get; } = parent;

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "aws.cdk.construct.v0");
        context.Writer.TryWriteString("construct-name", Name);

        var stack = Parent as IStackResource ?? this.Parent.SelectParentResource<IStackResource>();
        context.Writer.TryWriteString("stack-name", stack.StackName);
        context.Writer.TryWriteString("stack-unique-id", Construct.GetStackUniqueId());

        context.Writer.WritePropertyName("references");
        context.Writer.WriteStartArray();
        context.Writer.WriteStartObject();
        context.Writer.WriteString("parent-resource", Parent.Name);
        context.Writer.WriteEndObject();
        foreach (var constructResource in Annotations.OfType<ConstructReferenceAnnotation>())
        {
            context.Writer.WriteStartObject();
            context.Writer.WriteString("target-resource", constructResource.TargetResource);
            context.Writer.WriteString("output-name", constructResource.OutputName);
            context.Writer.WriteEndObject();
        }
        context.Writer.WriteEndArray();
    }
}

/// <inheritdoc cref="Aspire.Hosting.AWS.CDK.ConstructResource" />
internal sealed class ConstructResource<T>(string name, T construct, IResourceWithConstruct parent) : ConstructResource(name, construct, parent), IConstructResource<T>
    where T : IConstruct
{
    public new T Construct { get; } = construct;
}
