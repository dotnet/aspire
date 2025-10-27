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

    /// <summary>
    /// Converts the specified resource name into a Helm configuration section name.
    /// </summary>
    /// <remarks>
    /// The section names in Helm values files can not contain hyphens ('-').
    /// <see href="link">https://helm.sh/docs/chart_best_practices/values/</see>
    /// </remarks>
    public static string ToHelmValuesSectionName(this string resourceName)
        => $"{resourceName.Replace("-", "_")}";

    public static string ToHelmParameterExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{ParametersKey}.{resourceName}.{parameterName} }}}}".ToHelmValuesSectionName();

    public static string ToHelmSecretExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{SecretsKey}.{resourceName}.{parameterName} }}}}".ToHelmValuesSectionName();

    public static string ToHelmConfigExpression(this string parameterName, string resourceName)
        => $"{{{{ {ValuesSegment}.{ConfigKey}.{resourceName}.{parameterName} }}}}".ToHelmValuesSectionName();

    public static string ToHelmChartName(this string applicationName)
        => applicationName.ToLower().Replace("_", "-").Replace(".", "-");

    /// <summary>
    /// Converts the specified resource name into a Kubernetes resource name.
    /// </summary>
    /// <remarks>
    /// Kubernetes resource object names can only contain lowercase alphanumeric characters, '-', and '.'.
    /// <see href="link">https://kubernetes.io/docs/concepts/overview/working-with-objects/names/</see>
    /// </remarks>
    public static string ToKubernetesResourceName(this string resourceName)
        => $"{resourceName.ToLowerInvariant()}";

    public static string ToConfigMapName(this string resourceName)
        => $"{resourceName.ToKubernetesResourceName()}-{ConfigKey}";

    public static string ToSecretName(this string resourceName)
        => $"{resourceName.ToKubernetesResourceName()}-{SecretsKey}";

    public static string ToDeploymentName(this string resourceName)
        => $"{resourceName.ToKubernetesResourceName()}-{DeploymentKey}";

    public static string ToStatefulSetName(this string resourceName)
        => $"{resourceName.ToKubernetesResourceName()}-{StatefulSetKey}";

    public static string ToServiceName(this string resourceName)
        => $"{resourceName.ToKubernetesResourceName()}-{ServiceKey}";

    public static string ToPvcName(this string resourceName, string volumeName)
        => $"{resourceName.ToKubernetesResourceName()}-{volumeName}-{PvcKey}";

    public static string ToPvName(this string resourceName, string volumeName)
        => $"{resourceName.ToKubernetesResourceName()}-{volumeName}-{PvKey}";

    public static bool ContainsHelmExpression(this string value)
        => value.Contains($"{{{{ {ValuesSegment}.", StringComparison.Ordinal);

    public static bool ContainsHelmSecretExpression(this string value)
        => value.Contains($"{{{{ {ValuesSegment}.{SecretsKey}.", StringComparison.Ordinal);
}
