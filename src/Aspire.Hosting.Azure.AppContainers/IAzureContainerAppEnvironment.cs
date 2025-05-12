// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

internal interface IAzureContainerAppEnvironment
{
    IManifestExpressionProvider ContainerAppEnvironmentId { get; }
    IManifestExpressionProvider ContainerAppDomain { get; }
    IManifestExpressionProvider ContainerRegistryUrl { get; }
    IManifestExpressionProvider ContainerRegistryManagedIdentityId { get; }
    IManifestExpressionProvider LogAnalyticsWorkspaceId { get; }
    IManifestExpressionProvider PrincipalId { get; }
    IManifestExpressionProvider PrincipalName { get; }
    IManifestExpressionProvider ContainerAppEnvironmentName { get; }
    IManifestExpressionProvider GetSecretOutputKeyVault(AzureBicepResource resource);
    IManifestExpressionProvider GetVolumeStorage(IResource resource, ContainerMountAnnotation volume, int volumeIndex);
}
