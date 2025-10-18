// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Specifies an operator principal (user or group) that should receive administrative access to Azure resources
/// deployed from this compute environment.
/// </summary>
/// <param name="principalId">The Azure AD object ID of the user or group that will act as an operator.</param>
/// <remarks>
/// This annotation is applied to compute environment resources (e.g., Azure Container App Environments)
/// to designate principals that should receive elevated permissions on Azure resources created within that environment.
/// </remarks>
public class OperatorPrincipalAnnotation(IManifestExpressionProvider principalId) : IResourceAnnotation
{
    /// <summary>
    /// Gets the Azure AD object ID of the operator principal.
    /// </summary>
    public IManifestExpressionProvider PrincipalId { get; } = principalId;
}
