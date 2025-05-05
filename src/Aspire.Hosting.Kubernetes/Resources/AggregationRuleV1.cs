// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// Represents an aggregation rule for ClusterRoles in a Kubernetes environment.
/// Aggregation rules allow defining how to aggregate multiple cluster roles for
/// easier role-based access control (RBAC) management.
/// </summary>
[YamlSerializable]
public sealed class AggregationRuleV1
{
    /// <summary>
    /// Represents a collection of label selectors used to specify the aggregation behavior for cluster roles.
    /// Each label selector in the collection defines criteria to match certain cluster roles, allowing
    /// aggregation of permissions across multiple roles.
    /// </summary>
    [YamlMember(Alias = "clusterRoleSelectors")]
    public List<LabelSelectorV1> ClusterRoleSelectors { get; } = [];
}
