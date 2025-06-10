// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Extensions;

internal static class HelmExtensions
{
    private const string DeploymentKey = "deployment";
    private const string StatefulSetKey = "statefulset";
    private const string ServiceKey = "service";
    private const string PvcKey = "pvc";
    private const string PvKey = "pv";
    private const string ValuesSegment = ".Values";
    public const string ParametersKey = "parameters";
    public const string SecretsKey = "secrets";
    public const string ConfigKey = "config";
    public const string TemplateFileSeparator = "---";

    public static string ToManifestFriendlyResourceName(this string name)
        => name.Replace("-", "_");

    public static string ToHelmParameterExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{ParametersKey}.{resourceName}.{parameterName} }}}}";

    public static string ToHelmSecretExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{SecretsKey}.{resourceName}.{parameterName} }}}}";

    public static string ToHelmConfigExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{ConfigKey}.{resourceName}.{parameterName} }}}}";

    public static string ToConfigMapName(this string resourceName)
        => $"{resourceName}-{ConfigKey}";

    public static string ToSecretName(this string resourceName)
        => $"{resourceName}-{SecretsKey}";

    public static string ToDeploymentName(this string resourceName)
        => $"{resourceName}-{DeploymentKey}";

    public static string ToStatefulSetName(this string resourceName)
        => $"{resourceName}-{StatefulSetKey}";

    public static string ToServiceName(this string resourceName)
        => $"{resourceName}-{ServiceKey}";

    public static string ToPvcName(this string resourceName, string volumeName)
        => $"{resourceName}-{volumeName}-{PvcKey}";

    public static string ToPvName(this string resourceName, string volumeName)
        => $"{resourceName}-{volumeName}-{PvKey}";

    public static bool ContainsHelmExpression(this string value)
        => value.Contains($"{{{{ {ValuesSegment}.", StringComparison.Ordinal);

    public static bool ContainsHelmSecretExpression(this string value)
        => value.Contains($"{{{{ {ValuesSegment}.{SecretsKey}.", StringComparison.Ordinal);
}
