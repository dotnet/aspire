// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Kustomize;

internal static class KustomizeYamlKeys
{
    internal const string Resources = "resources";
    internal const string PatchesStrategicMerge = "patchesStrategicMerge";
    internal const string ConfigMapGenerator = "configMapGenerator";
}
