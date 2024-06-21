// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;
using Aspire.Hosting.AWS.Provisioning.Provisioners;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CloudFormationTemplateResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<CloudFormationTemplateResource>(loggerService, notificationService)
{
    protected override Task GetOrCreateResourceAsync(CloudFormationTemplateResource resource, CancellationToken cancellationToken)
        => ProvisionCloudFormationTemplateAsync(resource, cancellationToken);

    protected override async Task<CloudFormationStackExecutionContext> CreateCloudFormationExecutionContext(CloudFormationTemplateResource resource, CancellationToken cancellationToken)
    {
        var template = await File.ReadAllTextAsync(resource.TemplatePath, cancellationToken).ConfigureAwait(false);
        return new CloudFormationStackExecutionContext(resource.Name, template)
        {
            RoleArn = resource.RoleArn,
            DisableDiffCheck = resource.DisableDiffCheck,
            StackPollingInterval = resource.StackPollingInterval,
            DisabledCapabilities = resource.DisabledCapabilities,
            CloudFormationParameters = resource.CloudFormationParameters
        };
    }
}
