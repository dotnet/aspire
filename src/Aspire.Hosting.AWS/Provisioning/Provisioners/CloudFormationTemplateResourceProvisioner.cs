// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.AWS.CloudFormation;

namespace Aspire.Hosting.AWS.Provisioning;

internal sealed class CloudFormationTemplateResourceProvisioner(
    ResourceLoggerService loggerService,
    ResourceNotificationService notificationService)
    : CloudFormationResourceProvisioner<CloudFormationTemplateResource>(loggerService, notificationService)
{
    protected override Task GetOrCreateResourceAsync(CloudFormationTemplateResource resource, ProvisioningContext context, CancellationToken cancellationToken)
        => ProvisionCloudFormationTemplateAsync(resource, resource, cancellationToken);
}
