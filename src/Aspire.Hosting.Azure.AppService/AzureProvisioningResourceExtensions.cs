// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure.AppService;

internal static class AzureProvisioningResourceExtensions
{
    public static ProvisioningParameter AsProvisioningParameter(this IManifestExpressionProvider manifestExpressionProvider, AzureResourceInfrastructure infrastructure, string? parameterName = null, bool? isSecure = null)
    {
        ArgumentNullException.ThrowIfNull(manifestExpressionProvider);
        ArgumentNullException.ThrowIfNull(infrastructure);

        parameterName ??= GetNameFromValueExpression(manifestExpressionProvider);

        infrastructure.AspireResource.Parameters[parameterName] = manifestExpressionProvider;

        return GetOrAddParameter(infrastructure, parameterName, isSecure);
    }

    private static ProvisioningParameter GetOrAddParameter(AzureResourceInfrastructure infrastructure, string parameterName, bool? isSecure = null)
    {
        var parameter = infrastructure.GetProvisionableResources().OfType<ProvisioningParameter>().FirstOrDefault(p => p.BicepIdentifier == parameterName);
        if (parameter is null)
        {
            parameter = new ProvisioningParameter(parameterName, typeof(string));
            if (isSecure.HasValue)
            {
                parameter.IsSecure = isSecure.Value;
            };
            infrastructure.Add(parameter);
        }
        return parameter;
    }

    private static string GetNameFromValueExpression(IManifestExpressionProvider ep)
    {
        var parameterName = ep.ValueExpression.Replace("{", "").Replace("}", "").Replace(".", "_").Replace("-", "_").ToLowerInvariant();
        if (parameterName[0] == '_')
        {
            parameterName = parameterName[1..];
        }
        return parameterName;
    }
}