// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// This class encapsulates the Azure Container App environment details when it is provided
/// by azd. It implements the IAzureContainerAppEnvironment interface, which provides properties
/// and methods to access various environment-related information.
/// </summary>
internal sealed class AzdAzureContainerAppEnvironment : IAzureContainerAppEnvironment
{
    public IManifestExpressionProvider ContainerAppEnvironmentId => AzureContainerAppsEnvironment.AZURE_CONTAINER_APPS_ENVIRONMENT_ID;

    public IManifestExpressionProvider ContainerAppDomain => AzureContainerAppsEnvironment.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN;

    public IManifestExpressionProvider ContainerRegistryUrl => AzureContainerAppsEnvironment.AZURE_CONTAINER_REGISTRY_ENDPOINT;

    public IManifestExpressionProvider ContainerRegistryManagedIdentityId => AzureContainerAppsEnvironment.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID;

    public IManifestExpressionProvider LogAnalyticsWorkspaceId => AzureContainerAppsEnvironment.AZURE_LOG_ANALYTICS_WORKSPACE_ID;

    public IManifestExpressionProvider PrincipalId => AzureContainerAppsEnvironment.MANAGED_IDENTITY_PRINCIPAL_ID;

    public IManifestExpressionProvider PrincipalName => AzureContainerAppsEnvironment.MANAGED_IDENTITY_NAME;

    public IManifestExpressionProvider ContainerAppEnvironmentName => AzureContainerAppsEnvironment.AZURE_CONTAINER_APPS_ENVIRONMENT_NAME;

    public IManifestExpressionProvider GetSecretOutputKeyVault(AzureBicepResource resource)
    {
        return SecretOutputExpression.GetSecretOutputKeyVault(resource);
    }

    public IManifestExpressionProvider GetVolumeStorage(IResource resource, ContainerMountAnnotation volume, int volumeIndex)
    {
        return VolumeStorageExpression.GetVolumeStorage(resource, volume.Type, volumeIndex);
    }

    /// <summary>
    /// These are referencing outputs from azd's main.bicep file. We represent the global namespace in the manifest
    /// by using {.outputs.property} expressions.
    /// </summary>
    private sealed class AzureContainerAppsEnvironment(string outputName) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{.outputs.{outputName}}}";

        public static IManifestExpressionProvider MANAGED_IDENTITY_NAME => GetExpression("MANAGED_IDENTITY_NAME");
        public static IManifestExpressionProvider MANAGED_IDENTITY_PRINCIPAL_ID => GetExpression("MANAGED_IDENTITY_PRINCIPAL_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID => GetExpression("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_REGISTRY_ENDPOINT => GetExpression("AZURE_CONTAINER_REGISTRY_ENDPOINT");
        public static IManifestExpressionProvider AZURE_CONTAINER_REGISTRY_NAME => GetExpression("AZURE_CONTAINER_REGISTRY_NAME");
        public static IManifestExpressionProvider AZURE_CONTAINER_APPS_ENVIRONMENT_ID => GetExpression("AZURE_CONTAINER_APPS_ENVIRONMENT_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN => GetExpression("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN");
        public static IManifestExpressionProvider AZURE_LOG_ANALYTICS_WORKSPACE_ID => GetExpression("AZURE_LOG_ANALYTICS_WORKSPACE_ID");
        public static IManifestExpressionProvider AZURE_CONTAINER_APPS_ENVIRONMENT_NAME => GetExpression("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME");

        private static IManifestExpressionProvider GetExpression(string propertyExpression) =>
            new AzureContainerAppsEnvironment(propertyExpression);
    }

    /// <summary>
    /// Generates expressions for the secret outputs of the Azure Bicep resource. These are referencing outputs from azd's main.bicep file.
    /// </summary>
    /// <param name="resource"></param>
    private sealed class SecretOutputExpression(AzureBicepResource resource) : IManifestExpressionProvider
    {
        public string ValueExpression => $"{{{resource.Name}.secretOutputs}}";
        public static IManifestExpressionProvider GetSecretOutputKeyVault(AzureBicepResource resource) =>
            new SecretOutputExpression(resource);
    }

    /// <summary>
    /// Generates expressions for the volume storage account. That azd creates.
    /// </summary>
    private sealed class VolumeStorageExpression(IResource resource, ContainerMountType type, int index) : IManifestExpressionProvider
    {
        public string ValueExpression => type switch
        {
            ContainerMountType.BindMount => $"{{{resource.Name}.bindMounts.{index}.storage}}",
            ContainerMountType.Volume => $"{{{resource.Name}.volumes.{index}.storage}}",
            _ => throw new NotSupportedException()
        };

        public static IManifestExpressionProvider GetVolumeStorage(IResource resource, ContainerMountType type, int index) =>
            new VolumeStorageExpression(resource, type, index);
    }
}
