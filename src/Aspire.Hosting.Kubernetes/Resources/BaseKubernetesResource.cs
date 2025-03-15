// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Kubernetes.Yaml;
using Aspire.Hosting.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Serves as the foundational class for defining Kubernetes resources in the v1 API version.
/// </summary>
/// <remarks>
/// The BaseKubernetesResource class contains shared properties common to all Kubernetes resources,
/// such as Kind, ApiVersion, and Metadata. It acts as an abstract base for deriving specific
/// resource types and facilitates consistent handling of Kubernetes resource definitions.
/// </remarks>
[YamlSerializable]
public abstract class BaseKubernetesResource(string apiVersion, string kind) : BaseKubernetesObject(apiVersion, kind)
{
    /// <summary>
    /// Gets or sets the metadata for the Kubernetes resource.
    /// </summary>
    /// <remarks>
    /// The metadata contains standard information such as the resource’s name, namespace, labels, annotations,
    /// and other Kubernetes-specific properties. It is encapsulated in an <see cref="ObjectMetaV1"/> object,
    /// which provides properties for managing the resource’s unique identifier (UID), name, namespace, generation,
    /// and other relevant details like annotations, labels, and owner references.
    /// </remarks>
    [YamlMember(Alias = "metadata", Order = -1)]
    public ObjectMetaV1 Metadata { get; set; } = new();

    /// <summary>
    /// Converts the current Kubernetes resource object into its YAML representation.
    /// </summary>
    /// <param name="lineEndings">Specifies the line endings to be used in the YAML output. Defaults to a newline character ("\n").</param>
    /// <returns>A string representing the YAML-encoded content of the current resource object.</returns>
    public string ToYaml(string lineEndings = "\n")
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new ByteArrayStringYamlConverter())
            .WithEventEmitter(nextEmitter => new ForceQuotedStringsEventEmitter(nextEmitter))
            .WithEventEmitter(e => new FloatEmitter(e))
            .WithEmissionPhaseObjectGraphVisitor(args => new YamlIEnumerableSkipEmptyObjectGraphVisitor(args.InnerVisitor))
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .WithNewLine(lineEndings)
            .WithIndentedSequences()
            .Build();

        return serializer.Serialize(this);
    }
}
