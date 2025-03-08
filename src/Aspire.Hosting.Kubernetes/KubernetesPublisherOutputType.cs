// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents an output type for Kubernetes, such as Helm or Kustomize.
/// This class is used to define the format or approach for publishing
/// Kubernetes configurations.
/// </summary>
internal static class KubernetesPublisherOutputType
{
    public const string Helm = "helm";
    public const string Kustomize = "kustomize";

    public static string InvalidOutputTypeMessage(string outputType) =>
        $"""
        The '--output-type [type]' option was invalid. Supplied value was: '{outputType}'.
        "Valid values are '{Helm}' and '{Kustomize}'.
        """;

}
