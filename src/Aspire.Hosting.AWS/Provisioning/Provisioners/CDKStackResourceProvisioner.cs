// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Amazon.CDK.CXAPI;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Aspire.Hosting.AWS.Provisioning.Provisioners;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CDKStackResourceProvisioner<T>(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<T>(loggerService, notificationService)
    where T : IStackResource
{
    protected override async Task GetOrCreateResourceAsync(T resource, CancellationToken cancellationToken)
    {
        var logger = LoggerService.GetLogger(resource);
        await ProvisionCDKStackAssetsAsync(resource, logger).ConfigureAwait(false);
        await ProvisionCloudFormationTemplateAsync(resource, cancellationToken).ConfigureAwait(false);
    }

    private static Task ProvisionCDKStackAssetsAsync(T resource, ILogger logger)
    {
        var artifact = resource.Annotations.OfType<StackArtifactResourceAnnotation>().Single().StackArtifact;
        if (artifact.Dependencies
            .OfType<AssetManifestArtifact>()
            .Any(dependency =>
                dependency.Contents.Files?.Count > 1
                || dependency.Contents.DockerImages?.Count > 0))
        {
            logger.LogError("File or container image assets are currently not supported");
            throw new AWSProvisioningException("Failed to provision stack assets. Provisioning file or container image assets are currently not supported.");
        }

        return Task.CompletedTask;
    }

    protected override Task<CloudFormationStackExecutionContext> CreateCloudFormationExecutionContext(T resource, CancellationToken cancellationToken)
    {
        var artifact = resource.Annotations.OfType<StackArtifactResourceAnnotation>().Single().StackArtifact;
        var template = JsonSerializer.Serialize(artifact.Template);
        return Task.FromResult(new CloudFormationStackExecutionContext(resource.StackName, template));
    }
}
