// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.AWS.CloudFormation;

/// <summary>
/// The lifecycle hook that handles deploying the CloudFormation template to a CloudFormation stack.
/// </summary>
/// <param name="provisionerFactory"></param>
/// <param name="executionContext"></param>
/// <param name="notificationService"></param>
/// <param name="loggerService"></param>
internal sealed class CloudFormationLifecycleHook(
    ICloudFormationProvisionerFactory provisionerFactory,
    DistributedApplicationExecutionContext executionContext,
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{

    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsPublishMode)
        {
            return;
        }

        foreach (var cloudFormationResource in appModel.Resources.OfType<CloudFormationResource>())
        {
            await notificationService.PublishUpdateAsync(cloudFormationResource, (state) => state with { State = Constants.ResourceStateStarting }).ConfigureAwait(false);
            cloudFormationResource.ProvisioningTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // Create a CloudFormation provisioner
        var options = new CloudFormationProvisionerOptions(appModel, notificationService, loggerService);
        var provisioner = provisionerFactory.CreateProvisioner(options);
        // Run provisioner
        _ = Task.Run(() => provisioner.ProvisionCloudFormation(cancellationToken), cancellationToken);
    }
}

