// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents the configuration of ordinals for a StatefulSet in Kubernetes.
/// </summary>
/// <remarks>
/// The <c>StatefulSetOrdinalsV1</c> class defines optional settings that control
/// the starting ordinal for the replicas in a StatefulSet. Ordinals determine the numbering of the pods created
/// within a StatefulSet (e.g., pod-0, pod-1, etc.).
/// </remarks>
[YamlSerializable]
public sealed class StatefulSetOrdinalsV1
{
    /// <summary>
    /// Gets or sets the starting ordinal value for the StatefulSet instances.
    /// This property defines the initial index from which the StatefulSet instances will begin counting.
    /// </summary>
    [YamlMember(Alias = "start")]
    public int? Start { get; set; }
}
