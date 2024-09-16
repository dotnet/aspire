// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.CDK.CXAPI;
using Amazon.CloudFormation;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CDK;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CDKStackResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationTemplateResourceProvisioner<StackResource>(loggerService, notificationService)
{
    protected override async Task GetOrCreateResourceAsync(StackResource resource, CancellationToken cancellationToken)
    {
        var logger = LoggerService.GetLogger(resource);
        await ProvisionCDKStackAssetsAsync((StackResource)resource, logger).ConfigureAwait(false);
        await base.GetOrCreateResourceAsync(resource, cancellationToken).ConfigureAwait(false);
    }

    private static Task ProvisionCDKStackAssetsAsync(StackResource resource, ILogger logger)
    {
        // Currently CDK Stack Assets like S3 and Container images are not supported. When a stack contains those assets
        // we stop provisioning as it can introduce unwanted issues.
        if (!resource.TryGetStackArtifact(out var artifact))
        {
            throw new AWSProvisioningException("Failed to provision stack assets. Could not retrieve stack artifact.");
        }

        if (!artifact.Dependencies
                .OfType<AssetManifestArtifact>()
                .Any(dependency =>
                    dependency.Contents.Files?.Count > 1
                    || dependency.Contents.DockerImages?.Count > 0))
        {
            return Task.CompletedTask;
        }

        logger.LogError("File or container image assets are currently not supported");
        throw new AWSProvisioningException("Failed to provision stack assets. Provisioning file or container image assets are currently not supported.");
    }

    protected override void HandleTemplateProvisioningException(Exception ex, StackResource resource, ILogger logger)
    {
        if (ex.InnerException is AmazonCloudFormationException inner && inner.Message.StartsWith(@"Unable to fetch parameters [/cdk-bootstrap/"))
        {
            logger.LogError("The environment doesn't have the CDK toolkit stack installed. Use 'cdk boostrap' to setup your environment for use AWS CDK with Aspire");
        }
    }
}
